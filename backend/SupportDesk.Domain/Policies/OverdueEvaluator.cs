using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Time;

namespace SupportDesk.Domain.Policies;

/// <summary>
/// Derived overdue evaluation. Not persisted.
/// IsOverdue = utcNow &gt; dueDate AND status is New or InProgress.
/// </summary>
public static class OverdueEvaluator
{
    public static bool IsOverdue(DateTimeOffset dueDate, Status status, DateTimeOffset utcNow) =>
        utcNow > dueDate && TicketMutability.IsOpen(status);

    public static bool IsOverdue(DateTimeOffset dueDate, Status status, IClock clock) =>
        IsOverdue(dueDate, status, clock.UtcNow);
}
