export type Status = 'New' | 'InProgress' | 'Resolved' | 'Closed';

export type Priority = 'Low' | 'Normal' | 'High' | 'Critical';

export type Department = 'Technical' | 'Billing' | 'General';

export const STATUS_VALUES: readonly Status[] = [
  'New',
  'InProgress',
  'Resolved',
  'Closed',
] as const;

export const PRIORITY_VALUES: readonly Priority[] = [
  'Low',
  'Normal',
  'High',
  'Critical',
] as const;
