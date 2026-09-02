import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  of,
  skip,
  switchMap,
} from 'rxjs';
import { apiErrorMessage, extractApiError } from '../../core/errors/api-error.util';
import { ApiError } from '../../core/errors/api-error.model';
import { AgentService } from '../../agents/services/agent.service';
import { Agent } from '../../agents/models/agent.model';
import { formatOverdueRelative, priorityLabel, statusLabel } from '../../shared/models/display';
import { PRIORITY_VALUES, STATUS_VALUES, Priority, Status } from '../../shared/models/enums';
import { TicketListItem, TicketListQuery } from '../models/ticket-list.model';
import { TicketService } from '../services/ticket.service';

interface ListCriteria {
  search: string;
  status: '' | Status;
  priority: '' | Priority;
  assignedAgentId: string;
  overdueOnly: boolean;
  page: number;
  pageSize: number;
}

function isStatus(value: string | null): value is Status {
  return !!value && (STATUS_VALUES as readonly string[]).includes(value);
}

function isPriority(value: string | null): value is Priority {
  return !!value && (PRIORITY_VALUES as readonly string[]).includes(value);
}

@Component({
  selector: 'app-ticket-list-page',
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './ticket-list.page.html',
  styleUrl: './ticket-list.page.scss',
})
export class TicketListPage implements OnInit {
  private readonly tickets = inject(TicketService);
  private readonly agentsApi = inject(AgentService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly statusValues = STATUS_VALUES;
  readonly priorityValues = PRIORITY_VALUES;
  readonly statusLabel = statusLabel;
  readonly priorityLabel = priorityLabel;
  readonly formatOverdueRelative = formatOverdueRelative;

  readonly filterForm = this.fb.nonNullable.group({
    search: [''],
    status: ['' as '' | Status],
    priority: ['' as '' | Priority],
    assignedAgentId: [''],
    overdueOnly: [false],
  });

  readonly items = signal<TicketListItem[]>([]);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<ApiError | null>(null);
  readonly agents = signal<Agent[]>([]);
  /** Global overdue count from a one-shot lightweight query; not tied to filter changes. */
  readonly overdueCount = signal<number | null>(null);
  /** Set when arriving from Overview Open card (metric excludes Closed; list API cannot). */
  readonly openContext = signal(false);

  private readonly criteria$ = new BehaviorSubject<ListCriteria>({
    search: '',
    status: '',
    priority: '',
    assignedAgentId: '',
    overdueOnly: false,
    page: 1,
    pageSize: 20,
  });

  ngOnInit(): void {
    // Apply deep-link / Overview card filters before the first list request.
    this.applyQueryParams(this.route.snapshot.queryParamMap);

    this.agentsApi
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => this.agents.set(list),
        error: () => undefined,
      });

    // Overdue metric: single lightweight request on enter (pageSize=1 → totalCount only).
    this.loadOverdueMetric();

    this.filterForm.controls.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        const current = this.criteria$.value;
        this.criteria$.next({ ...current, search: search.trim(), page: 1 });
      });

    this.filterForm.controls.status.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((status) => {
        this.openContext.set(false);
        const current = this.criteria$.value;
        this.criteria$.next({ ...current, status, page: 1 });
      });

    this.filterForm.controls.priority.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((priority) => {
        this.openContext.set(false);
        const current = this.criteria$.value;
        this.criteria$.next({ ...current, priority, page: 1 });
      });

    this.filterForm.controls.assignedAgentId.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((assignedAgentId) => {
        const current = this.criteria$.value;
        this.criteria$.next({ ...current, assignedAgentId, page: 1 });
      });

    this.filterForm.controls.overdueOnly.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((overdueOnly) => {
        this.openContext.set(false);
        const current = this.criteria$.value;
        this.criteria$.next({ ...current, overdueOnly, page: 1 });
      });

    this.criteria$
      .pipe(
        switchMap((criteria) => {
          this.loading.set(true);
          this.error.set(null);
          this.page.set(criteria.page);
          return this.tickets.list(this.toQuery(criteria)).pipe(
            catchError((err) => {
              this.error.set(extractApiError(err));
              this.items.set([]);
              this.totalCount.set(0);
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.loading.set(false);
        if (!result) {
          return;
        }
        this.items.set(result.items);
        this.page.set(result.page);
        this.pageSize.set(result.pageSize);
        this.totalCount.set(result.totalCount);
      });

    // Subsequent Overview card clicks while Tickets is already open.
    this.route.queryParamMap
      .pipe(skip(1), takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
        this.applyQueryParams(params);
      });
  }

  private applyQueryParams(params: {
    get(name: string): string | null;
  }): void {
    const statusParam = params.get('status');
    const priorityParam = params.get('priority');
    const overdueOnly = params.get('overdueOnly') === 'true';
    const open = params.get('open') === '1';

    const status: '' | Status = isStatus(statusParam) ? statusParam : '';
    const priority: '' | Priority = isPriority(priorityParam) ? priorityParam : '';

    // open=1: clear filters and fetch all tickets. Open metric = Total−Closed (no exclude-Closed API).
    if (open && !status && !priority && !overdueOnly) {
      this.openContext.set(true);
      this.filterForm.setValue(
        { search: '', status: '', priority: '', assignedAgentId: '', overdueOnly: false },
        { emitEvent: false },
      );
      this.criteria$.next({
        search: '',
        status: '',
        priority: '',
        assignedAgentId: '',
        overdueOnly: false,
        page: 1,
        pageSize: 20,
      });
      return;
    }

    this.openContext.set(false);
    this.filterForm.setValue(
      {
        search: '',
        status,
        priority,
        assignedAgentId: '',
        overdueOnly,
      },
      { emitEvent: false },
    );
    this.criteria$.next({
      search: '',
      status,
      priority,
      assignedAgentId: '',
      overdueOnly,
      page: 1,
      pageSize: 20,
    });
  }

  private loadOverdueMetric(): void {
    this.tickets
      .list({ page: 1, pageSize: 1, overdueOnly: true })
      .pipe(
        catchError(() => of(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.overdueCount.set(result ? result.totalCount : null);
      });
  }

  private toQuery(c: ListCriteria): TicketListQuery {
    return {
      page: c.page,
      pageSize: c.pageSize,
      search: c.search || null,
      status: c.status || null,
      priority: c.priority || null,
      assignedAgentId: c.assignedAgentId || null,
      overdueOnly: c.overdueOnly || undefined,
    };
  }

  errorMessage(): string {
    const err = this.error();
    return err ? apiErrorMessage(err) : '';
  }

  totalPages(): number {
    const size = this.pageSize() || 20;
    return Math.max(1, Math.ceil(this.totalCount() / size));
  }

  goToPage(next: number): void {
    const clamped = Math.min(Math.max(1, next), this.totalPages());
    const current = this.criteria$.value;
    if (clamped === current.page) {
      return;
    }
    this.criteria$.next({ ...current, page: clamped });
  }

  retry(): void {
    const current = this.criteria$.value;
    this.criteria$.next({ ...current });
  }

  filtersActive(): boolean {
    const v = this.filterForm.getRawValue();
    return (
      v.search.trim() !== '' ||
      v.status !== '' ||
      v.priority !== '' ||
      v.assignedAgentId !== '' ||
      v.overdueOnly
    );
  }

  clearFilters(): void {
    this.openContext.set(false);
    this.filterForm.setValue(
      {
        search: '',
        status: '',
        priority: '',
        assignedAgentId: '',
        overdueOnly: false,
      },
      { emitEvent: false },
    );
    const current = this.criteria$.value;
    this.criteria$.next({
      ...current,
      search: '',
      status: '',
      priority: '',
      assignedAgentId: '',
      overdueOnly: false,
      page: 1,
    });
    void this.router.navigate(['/tickets']);
  }

  toggleOverdueFilter(): void {
    this.openContext.set(false);
    const next = !this.filterForm.controls.overdueOnly.value;
    this.filterForm.controls.overdueOnly.setValue(next);
  }

  isOverdueFiltered(): boolean {
    return this.filterForm.controls.overdueOnly.value;
  }

  openTicket(id: string): void {
    void this.router.navigate(['/tickets', id]);
  }

  trackById(_index: number, item: TicketListItem): string {
    return item.id;
  }
}
