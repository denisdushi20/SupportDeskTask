using SupportDesk.Domain.Enums;

namespace SupportDesk.Domain.Policies;

/// <summary>
/// Single reusable mutability rules for tickets.
/// Open = New | InProgress. Closed is fully immutable.
/// </summary>
public static class TicketMutability
{
    public static bool IsOpen(Status status) =>
        status is Status.New or Status.InProgress;

    public static bool IsClosed(Status status) =>
        status == Status.Closed;

    /// <summary>Any mutating operation is forbidden when Closed.</summary>
    public static bool CanMutate(Status status) =>
        !IsClosed(status);

    /// <summary>Editable fields (including priority) only while open.</summary>
    public static bool CanEditFields(Status status) =>
        IsOpen(status);

    public static PolicyDecision EnsureNotClosed(Status status)
    {
        if (IsClosed(status))
        {
            return PolicyDecision.Reject(
                DomainErrorCodes.TicketClosed,
                "Closed tickets are read-only and cannot be modified.");
        }

        return PolicyDecision.Allow();
    }

    public static PolicyDecision EnsureCanEditFields(Status status)
    {
        var closed = EnsureNotClosed(status);
        if (!closed.IsAllowed)
        {
            return closed;
        }

        if (!CanEditFields(status))
        {
            return PolicyDecision.Reject(
                DomainErrorCodes.TicketNotEditable,
                $"Ticket fields cannot be edited when status is {status}.");
        }

        return PolicyDecision.Allow();
    }
}
