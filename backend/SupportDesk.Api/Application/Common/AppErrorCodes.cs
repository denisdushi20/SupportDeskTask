namespace SupportDesk.Api.Application.Common;

public static class AppErrorCodes
{
    public const string TicketNotFound = "TICKET_NOT_FOUND";
    public const string AgentNotFound = "AGENT_NOT_FOUND";
    public const string AgentInactive = "AGENT_INACTIVE";
    public const string AssignmentRequired = "ASSIGNMENT_REQUIRED";
    public const string InvalidStatusTransition = "INVALID_STATUS_TRANSITION";
    public const string TicketClosed = "TICKET_CLOSED";
    public const string TicketNotEditable = "TICKET_NOT_EDITABLE";
    public const string AgentHasTickets = "AGENT_HAS_TICKETS";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Conflict = "CONFLICT";
}
