import { Department, Priority, Status } from './enums';

const STATUS_LABELS: Record<Status, string> = {
  New: 'New',
  InProgress: 'In Progress',
  Resolved: 'Resolved',
  Closed: 'Closed',
};

const PRIORITY_LABELS: Record<Priority, string> = {
  Low: 'Low',
  Normal: 'Normal',
  High: 'High',
  Critical: 'Critical',
};

const DEPARTMENT_LABELS: Record<Department, string> = {
  Technical: 'Technical',
  Billing: 'Billing',
  General: 'General',
};

/** Display-only labels. Not a workflow/state machine. */
export function statusLabel(status: Status): string {
  return STATUS_LABELS[status] ?? status;
}

export function priorityLabel(priority: Priority): string {
  return PRIORITY_LABELS[priority] ?? priority;
}

export function departmentLabel(department: Department): string {
  return DEPARTMENT_LABELS[department] ?? department;
}

/**
 * Display-only action labels for backend-authorized transitions.
 * Does not decide which transitions are allowed — use allowedTransitions for that.
 */
export function transitionActionLabel(currentStatus: Status, nextStatus: Status): string {
  if (nextStatus === 'InProgress' && currentStatus === 'Resolved') {
    return 'Reopen';
  }
  if (nextStatus === 'InProgress') {
    return 'Start work';
  }
  if (nextStatus === 'Resolved') {
    return 'Resolve';
  }
  if (nextStatus === 'Closed') {
    return 'Close';
  }
  return `Move to ${statusLabel(nextStatus)}`;
}

/** Presentation helper: relative overdue duration from dueDate. Server isOverdue remains authoritative. */
export function formatOverdueRelative(dueDateIso: string, now: Date = new Date()): string | null {
  const due = new Date(dueDateIso);
  if (Number.isNaN(due.getTime())) {
    return null;
  }
  const ms = now.getTime() - due.getTime();
  if (ms <= 0) {
    return null;
  }
  const minutes = Math.floor(ms / 60_000);
  if (minutes < 60) {
    return `${Math.max(1, minutes)}m overdue`;
  }
  const hours = Math.floor(minutes / 60);
  if (hours < 48) {
    const rem = minutes % 60;
    return rem > 0 ? `${hours}h ${rem}m overdue` : `${hours}h overdue`;
  }
  const days = Math.floor(hours / 24);
  return `${days}d overdue`;
}
