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
