using SupportDesk.Domain.Enums;

namespace SupportDesk.Domain.Policies;

/// <summary>
/// Pure structural status-machine policy.
/// Answers only whether a status transition is structurally allowed.
/// Does not load agents, query databases, or enforce assignment rules.
/// </summary>
public static class TicketTransitionPolicy
{
    private static readonly HashSet<(Status From, Status To)> AllowedTransitions =
    [
        (Status.New, Status.InProgress),
        (Status.InProgress, Status.Resolved),
        (Status.Resolved, Status.Closed),
        (Status.Resolved, Status.InProgress)
    ];

    public static bool IsAllowed(Status current, Status requested) =>
        AllowedTransitions.Contains((current, requested));

    public static PolicyDecision Evaluate(Status current, Status requested)
    {
        if (IsAllowed(current, requested))
        {
            return PolicyDecision.Allow();
        }

        if (current == Status.Closed)
        {
            return PolicyDecision.Reject(
                DomainErrorCodes.TicketClosed,
                $"Closed tickets cannot transition from {current} to {requested}.");
        }

        return PolicyDecision.Reject(
            DomainErrorCodes.InvalidStatusTransition,
            $"Transition from {current} to {requested} is not allowed.");
    }
}
