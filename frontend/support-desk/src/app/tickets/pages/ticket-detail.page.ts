import { DatePipe } from '@angular/common';
import { Component, DestroyRef, HostListener, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { Agent } from '../../agents/models/agent.model';
import { AgentService } from '../../agents/services/agent.service';
import { ApiError } from '../../core/errors/api-error.model';
import { apiErrorMessage, extractApiError } from '../../core/errors/api-error.util';
import {
  departmentLabel,
  formatOverdueRelative,
  priorityLabel,
  statusLabel,
  transitionActionLabel,
} from '../../shared/models/display';
import { Status } from '../../shared/models/enums';
import { isGuidFormat } from '../../shared/models/guid';
import { TicketDetail } from '../models/ticket-detail.model';
import { TicketService } from '../services/ticket.service';

function nonWhitespace(control: { value: unknown }) {
  const v = typeof control.value === 'string' ? control.value.trim() : '';
  return v.length > 0 ? null : { whitespace: true };
}

@Component({
  selector: 'app-ticket-detail-page',
  imports: [DatePipe, RouterLink, ReactiveFormsModule],
  templateUrl: './ticket-detail.page.html',
  styleUrl: './ticket-detail.page.scss',
})
export class TicketDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly tickets = inject(TicketService);
  private readonly agentsApi = inject(AgentService);
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly statusLabel = statusLabel;
  readonly priorityLabel = priorityLabel;
  readonly departmentLabel = departmentLabel;
  readonly transitionActionLabel = transitionActionLabel;
  readonly formatOverdueRelative = formatOverdueRelative;

  readonly ticket = signal<TicketDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<ApiError | null>(null);
  readonly actionError = signal<ApiError | null>(null);
  readonly actionBusy = signal(false);
  readonly agents = signal<Agent[]>([]);
  readonly showDeleteConfirm = signal(false);

  readonly assignForm = this.fb.nonNullable.group({
    agentId: ['', Validators.required],
  });

  readonly commentForm = this.fb.nonNullable.group({
    authorName: ['', [Validators.required, Validators.maxLength(200), nonWhitespace]],
    body: ['', [Validators.required, Validators.maxLength(4000), nonWhitespace]],
  });

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id') ?? '';
          this.loading.set(true);
          this.error.set(null);
          this.actionError.set(null);
          this.ticket.set(null);

          if (!isGuidFormat(id)) {
            this.loading.set(false);
            this.error.set({
              status: 400,
              code: 'INVALID_ID',
              detail: 'The ticket id in the URL is not valid.',
            });
            return [];
          }

          return this.tickets.get(id);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (detail) => {
          this.loading.set(false);
          this.ticket.set(detail);
          this.syncAssignSelection(detail);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(extractApiError(err));
        },
      });

    this.agentsApi
      .list()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => this.agents.set(list),
        error: () => undefined,
      });
  }

  activeAgents(): Agent[] {
    return this.agents().filter((a) => a.active);
  }

  errorMessage(err: ApiError | null = this.error()): string {
    return err ? apiErrorMessage(err) : '';
  }

  changeStatus(status: Status): void {
    const current = this.ticket();
    if (!current || this.actionBusy()) {
      return;
    }
    this.actionBusy.set(true);
    this.actionError.set(null);
    this.tickets
      .changeStatus(current.id, status)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.actionBusy.set(false);
          this.ticket.set(updated);
          this.syncAssignSelection(updated);
        },
        error: (err) => {
          this.actionBusy.set(false);
          this.actionError.set(extractApiError(err));
        },
      });
  }

  assign(): void {
    const current = this.ticket();
    if (!current || !current.canAssign || this.assignForm.invalid || this.actionBusy()) {
      this.assignForm.markAllAsTouched();
      return;
    }
    this.actionBusy.set(true);
    this.actionError.set(null);
    const agentId = this.assignForm.controls.agentId.value;
    this.tickets
      .assign(current.id, agentId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (updated) => {
          this.actionBusy.set(false);
          this.ticket.set(updated);
          this.syncAssignSelection(updated);
        },
        error: (err) => {
          this.actionBusy.set(false);
          this.actionError.set(extractApiError(err));
        },
      });
  }

  unassign(): void {
    const current = this.ticket();
    if (!current || !current.canUnassign || this.actionBusy()) {
      return;
    }
    this.actionBusy.set(true);
    this.actionError.set(null);
    this.tickets
      .unassign(current.id)
      .pipe(
        switchMap(() => this.tickets.get(current.id)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.actionBusy.set(false);
          this.ticket.set(updated);
          this.syncAssignSelection(updated);
        },
        error: (err) => {
          this.actionBusy.set(false);
          this.actionError.set(extractApiError(err));
        },
      });
  }

  addComment(): void {
    const current = this.ticket();
    if (!current || !current.canAddComment || this.commentForm.invalid || this.actionBusy()) {
      this.commentForm.markAllAsTouched();
      return;
    }
    this.actionBusy.set(true);
    this.actionError.set(null);
    const { authorName, body } = this.commentForm.getRawValue();
    this.tickets
      .addComment(current.id, {
        authorName: authorName.trim(),
        body: body.trim(),
      })
      .pipe(
        switchMap(() => this.tickets.get(current.id)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updated) => {
          this.actionBusy.set(false);
          this.ticket.set(updated);
          this.commentForm.reset({ authorName: '', body: '' });
        },
        error: (err) => {
          this.actionBusy.set(false);
          this.actionError.set(extractApiError(err));
        },
      });
  }

  confirmDelete(): void {
    this.showDeleteConfirm.set(true);
  }

  cancelDelete(): void {
    this.showDeleteConfirm.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.showDeleteConfirm()) {
      this.cancelDelete();
    }
  }

  deleteTicket(): void {
    const current = this.ticket();
    if (!current || !current.canDelete || this.actionBusy()) {
      return;
    }
    this.actionBusy.set(true);
    this.actionError.set(null);
    this.tickets
      .delete(current.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.actionBusy.set(false);
          this.showDeleteConfirm.set(false);
          void this.router.navigate(['/tickets']);
        },
        error: (err) => {
          this.actionBusy.set(false);
          this.actionError.set(extractApiError(err));
          this.showDeleteConfirm.set(false);
        },
      });
  }

  private syncAssignSelection(detail: TicketDetail): void {
    const control = this.assignForm.controls.agentId;
    control.setValue(detail.assignedAgentId ?? '');
    if (detail.canAssign) {
      control.enable({ emitEvent: false });
    } else {
      control.disable({ emitEvent: false });
    }
  }
}
