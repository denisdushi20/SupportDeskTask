export interface ApiError {
  status: number;
  title?: string;
  detail?: string;
  code?: string;
  fieldErrors?: Record<string, string[]>;
  currentStatus?: string;
  requestedStatus?: string;
  reason?: string;
  context?: Record<string, unknown>;
}

export const ApiErrorCodes = {
  ValidationError: 'VALIDATION_ERROR',
  TicketClosed: 'TICKET_CLOSED',
  TicketNotEditable: 'TICKET_NOT_EDITABLE',
  AgentInactive: 'AGENT_INACTIVE',
  AssignmentRequired: 'ASSIGNMENT_REQUIRED',
  InvalidStatusTransition: 'INVALID_STATUS_TRANSITION',
  TicketNotFound: 'TICKET_NOT_FOUND',
  AgentNotFound: 'AGENT_NOT_FOUND',
  Conflict: 'CONFLICT',
} as const;
