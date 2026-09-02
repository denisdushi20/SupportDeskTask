namespace SupportDesk.Domain.Policies;

/// <summary>
/// Assignment eligibility rules that do not depend on persistence.
/// Contextual checks such as closed-ticket guards belong with the application service.
/// </summary>
public static class TicketAssignmentPolicy
{
    public static PolicyDecision EvaluateAssign(bool agentIsActive)
    {
        if (!agentIsActive)
        {
            return PolicyDecision.Reject(
                DomainErrorCodes.AgentInactive,
                "An inactive agent cannot be assigned to a ticket.");
        }

        return PolicyDecision.Allow();
    }
}
