namespace SupportDesk.Domain.Policies;

public static class DomainErrorCodes
{
    public const string InvalidStatusTransition = "INVALID_STATUS_TRANSITION";
    public const string AgentInactive = "AGENT_INACTIVE";
    public const string TicketClosed = "TICKET_CLOSED";
    public const string TicketNotEditable = "TICKET_NOT_EDITABLE";
    public const string AssignmentRequired = "ASSIGNMENT_REQUIRED";
}
