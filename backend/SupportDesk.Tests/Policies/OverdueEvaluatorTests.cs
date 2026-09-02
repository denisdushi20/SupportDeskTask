using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Policies;
using SupportDesk.Tests.TestSupport;

namespace SupportDesk.Tests.Policies;

public class OverdueEvaluatorTests
{
    private static readonly DateTimeOffset DueDate =
        new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Before_due_date_is_not_overdue()
    {
        var clock = new FakeClock(DueDate.AddMinutes(-1));

        Assert.False(OverdueEvaluator.IsOverdue(DueDate, Status.New, clock));
        Assert.False(OverdueEvaluator.IsOverdue(DueDate, Status.InProgress, clock));
    }

    [Fact]
    public void Exactly_at_due_date_is_not_overdue()
    {
        // Protects CurrentUtc > DueDate (not >=). Equality must not count as overdue.
        var clock = new FakeClock(DueDate);

        Assert.False(OverdueEvaluator.IsOverdue(DueDate, Status.New, clock));
        Assert.False(OverdueEvaluator.IsOverdue(DueDate, Status.InProgress, clock));
    }

    [Fact]
    public void After_due_date_with_New_is_overdue()
    {
        var clock = new FakeClock(DueDate.AddMinutes(1));

        Assert.True(OverdueEvaluator.IsOverdue(DueDate, Status.New, clock));
    }

    [Fact]
    public void After_due_date_with_InProgress_is_overdue()
    {
        var clock = new FakeClock(DueDate.AddMinutes(1));

        Assert.True(OverdueEvaluator.IsOverdue(DueDate, Status.InProgress, clock));
    }

    [Fact]
    public void After_due_date_with_Resolved_is_not_overdue()
    {
        var clock = new FakeClock(DueDate.AddDays(1));

        Assert.False(OverdueEvaluator.IsOverdue(DueDate, Status.Resolved, clock));
    }

    [Fact]
    public void After_due_date_with_Closed_is_not_overdue()
    {
        var clock = new FakeClock(DueDate.AddDays(1));

        Assert.False(OverdueEvaluator.IsOverdue(DueDate, Status.Closed, clock));
    }
}
