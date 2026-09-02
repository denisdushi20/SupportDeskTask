import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { ApiError } from '../../core/errors/api-error.model';
import { apiErrorMessage, extractApiError } from '../../core/errors/api-error.util';
import { formatOverdueRelative, priorityLabel, statusLabel } from '../../shared/models/display';
import { TicketListItem } from '../../tickets/models/ticket-list.model';
import { TicketService } from '../../tickets/services/ticket.service';
import {
  StatusShare,
  buildStatusShares,
  computeOpenCount,
} from '../utils/overview-metrics';

@Component({
  selector: 'app-overview-page',
  imports: [RouterLink, DatePipe],
  templateUrl: './overview.page.html',
  styleUrl: './overview.page.scss',
})
export class OverviewPage implements OnInit {
  private readonly tickets = inject(TicketService);
  private readonly destroyRef = inject(DestroyRef);

  readonly priorityLabel = priorityLabel;
  readonly statusLabel = statusLabel;
  readonly formatOverdueRelative = formatOverdueRelative;

  readonly loading = signal(true);
  readonly error = signal<ApiError | null>(null);

  /** Unfiltered total — independent of Tickets page filter state. */
  readonly totalCount = signal<number | null>(null);
  readonly overdueCount = signal<number | null>(null);
  readonly criticalCount = signal<number | null>(null);
  readonly openCount = signal<number | null>(null);
  readonly needsAttention = signal<TicketListItem[]>([]);
  readonly statusShares = signal<StatusShare[]>([]);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);

    // One-shot forkJoin. Counts use pageSize=1 totalCount. Total has no filters.
    forkJoin({
      total: this.tickets.list({ page: 1, pageSize: 1 }),
      overdue: this.tickets.list({ page: 1, pageSize: 1, overdueOnly: true }),
      critical: this.tickets.list({ page: 1, pageSize: 1, priority: 'Critical' }),
      closed: this.tickets.list({ page: 1, pageSize: 1, status: 'Closed' }),
      statusNew: this.tickets.list({ page: 1, pageSize: 1, status: 'New' }),
      statusInProgress: this.tickets.list({ page: 1, pageSize: 1, status: 'InProgress' }),
      statusResolved: this.tickets.list({ page: 1, pageSize: 1, status: 'Resolved' }),
      attention: this.tickets.list({ page: 1, pageSize: 5, overdueOnly: true }),
    })
      .pipe(
        catchError((err) => {
          this.error.set(extractApiError(err));
          return of(null);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.loading.set(false);
        if (!result) {
          this.totalCount.set(null);
          this.overdueCount.set(null);
          this.criticalCount.set(null);
          this.openCount.set(null);
          this.needsAttention.set([]);
          this.statusShares.set([]);
          return;
        }
        this.totalCount.set(result.total.totalCount);
        this.overdueCount.set(result.overdue.totalCount);
        this.criticalCount.set(result.critical.totalCount);
        this.openCount.set(computeOpenCount(result.total.totalCount, result.closed.totalCount));
        this.needsAttention.set(result.attention.items);
        this.statusShares.set(
          buildStatusShares({
            New: result.statusNew.totalCount,
            InProgress: result.statusInProgress.totalCount,
            Resolved: result.statusResolved.totalCount,
            Closed: result.closed.totalCount,
          }),
        );
      });
  }

  errorMessage(): string {
    const err = this.error();
    return err ? apiErrorMessage(err) : '';
  }
}
