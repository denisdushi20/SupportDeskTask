import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  debounceTime,
  distinctUntilChanged,
  of,
  switchMap,
} from 'rxjs';
import { apiErrorMessage, extractApiError } from '../../core/errors/api-error.util';
import { ApiError } from '../../core/errors/api-error.model';
import { AgentService } from '../../agents/services/agent.service';
import { Agent } from '../../agents/models/agent.model';
import { priorityLabel, statusLabel } from '../../shared/models/display';
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

  readonly statusValues = STATUS_VALUES;
  readonly priorityValues = PRIORITY_VALUES;
  readonly statusLabel = statusLabel;
  readonly priorityLabel = priorityLabel;

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
    this.agentsApi
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => this.agents.set(list),
        error: () => undefined,
      });

    this.filterForm.controls.search.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((search) => {
        const current = this.criteria$.value;
        this.criteria$.next({ ...current, search: search.trim(), page: 1 });
      });

    this.filterForm.controls.status.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((status) => {
        const current = this.criteria$.value;
        this.criteria$.next({ ...current, status, page: 1 });
      });

    this.filterForm.controls.priority.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((priority) => {
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

  trackById(_index: number, item: TicketListItem): string {
    return item.id;
  }
}
