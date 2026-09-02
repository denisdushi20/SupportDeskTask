import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { TicketDetail } from '../models/ticket-detail.model';
import { priorityLabel, statusLabel } from '../../shared/models/display';
import { Status } from '../../shared/models/enums';

/**
 * Focused presentational harness mirroring detail-page mutability rules
 * driven by backend capability flags and allowedTransitions.
 */
@Component({
  selector: 'app-ticket-mutability-harness',
  standalone: true,
  template: `
    @if (ticket.status === 'Closed') {
      <div class="banner-closed">This ticket is closed and immutable.</div>
    }
    @if (ticket.status === 'Resolved') {
      <div class="banner-resolved">
        Resolved — fields are locked, but workflow actions remain available.
      </div>
    }

    <a class="edit-link" [attr.aria-disabled]="!ticket.canEditFields">Edit</a>
    <button type="button" class="delete-btn" [disabled]="!ticket.canDelete">Delete</button>

    <div class="status-actions">
      @for (next of ticket.allowedTransitions; track next) {
        <button type="button" class="status-btn" [attr.data-status]="next">
          Move to {{ statusLabel(next) }}
        </button>
      }
    </div>

    <button type="button" class="assign-btn" [disabled]="!ticket.canAssign">Assign</button>
    <button type="button" class="unassign-btn" [disabled]="!ticket.canUnassign">Unassign</button>
    <button type="button" class="comment-btn" [disabled]="!ticket.canAddComment">
      Add comment
    </button>
  `,
})
class TicketMutabilityHarness {
  readonly statusLabel = statusLabel;
  readonly priorityLabel = priorityLabel;
  ticket!: TicketDetail;
}

function baseTicket(overrides: Partial<TicketDetail>): TicketDetail {
  return {
    id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    reference: 'TKT-0001',
    title: 'Sample',
    description: 'Desc',
    customerName: 'Ada',
    customerEmail: 'ada@example.com',
    priority: 'Normal',
    status: 'New',
    assignedAgentId: null,
    assignedAgent: null,
    createdDate: '2026-01-01T00:00:00+00:00',
    lastModifiedDate: '2026-01-01T00:00:00+00:00',
    resolvedDate: null,
    closedDate: null,
    dueDate: '2026-01-03T00:00:00+00:00',
    isOverdue: false,
    allowedTransitions: ['InProgress'],
    canEditFields: true,
    canAssign: true,
    canUnassign: true,
    canAddComment: true,
    canDelete: true,
    comments: [],
    ...overrides,
  };
}

describe('Ticket mutability UI (capability-driven)', () => {
  let fixture: ComponentFixture<TicketMutabilityHarness>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TicketMutabilityHarness],
    }).compileComponents();
    fixture = TestBed.createComponent(TicketMutabilityHarness);
  });

  it('disables mutation controls when capabilities indicate Closed', () => {
    fixture.componentInstance.ticket = baseTicket({
      status: 'Closed',
      allowedTransitions: [],
      canEditFields: false,
      canAssign: false,
      canUnassign: false,
      canAddComment: false,
      canDelete: false,
    });
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.banner-closed')?.textContent).toContain('closed and immutable');
    expect(el.querySelector('.edit-link')?.getAttribute('aria-disabled')).toBe('true');
    expect((el.querySelector('.delete-btn') as HTMLButtonElement).disabled).toBeTrue();
    expect((el.querySelector('.assign-btn') as HTMLButtonElement).disabled).toBeTrue();
    expect((el.querySelector('.unassign-btn') as HTMLButtonElement).disabled).toBeTrue();
    expect((el.querySelector('.comment-btn') as HTMLButtonElement).disabled).toBeTrue();
    expect(el.querySelectorAll('.status-btn').length).toBe(0);
  });

  it('renders only backend-provided allowedTransitions', () => {
    const transitions: Status[] = ['Closed', 'InProgress'];
    fixture.componentInstance.ticket = baseTicket({
      status: 'Resolved',
      allowedTransitions: transitions,
      canEditFields: false,
      canAssign: true,
      canUnassign: true,
      canAddComment: true,
      canDelete: true,
    });
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.banner-resolved')).toBeTruthy();
    expect((el.querySelector('.comment-btn') as HTMLButtonElement).disabled).toBeFalse();
    expect((el.querySelector('.assign-btn') as HTMLButtonElement).disabled).toBeFalse();
    expect(el.querySelector('.edit-link')?.getAttribute('aria-disabled')).toBe('true');

    const buttons = fixture.debugElement.queryAll(By.css('.status-btn'));
    expect(buttons.length).toBe(2);
    const statuses = buttons.map((b) => b.attributes['data-status']);
    expect(statuses).toEqual(['Closed', 'InProgress']);
    expect(buttons[0].nativeElement.textContent).toContain('Closed');
    expect(buttons[1].nativeElement.textContent).toContain('In Progress');
  });
});
