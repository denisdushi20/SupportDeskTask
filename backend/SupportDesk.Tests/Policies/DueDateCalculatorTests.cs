using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Policies;

namespace SupportDesk.Tests.Policies;

public class DueDateCalculatorTests
{
    private static readonly DateTimeOffset Created =
        new(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Critical_is_created_plus_four_hours()
    {
        var due = DueDateCalculator.Calculate(Created, Priority.Critical);

        Assert.Equal(new DateTimeOffset(2026, 9, 2, 14, 0, 0, TimeSpan.Zero), due);
    }

    [Fact]
    public void High_is_created_plus_one_day()
    {
        var due = DueDateCalculator.Calculate(Created, Priority.High);

        Assert.Equal(new DateTimeOffset(2026, 9, 3, 10, 0, 0, TimeSpan.Zero), due);
    }

    [Fact]
    public void Normal_is_created_plus_three_days()
    {
        var due = DueDateCalculator.Calculate(Created, Priority.Normal);

        Assert.Equal(new DateTimeOffset(2026, 9, 5, 10, 0, 0, TimeSpan.Zero), due);
    }

    [Fact]
    public void Low_is_created_plus_seven_days()
    {
        var due = DueDateCalculator.Calculate(Created, Priority.Low);

        Assert.Equal(new DateTimeOffset(2026, 9, 9, 10, 0, 0, TimeSpan.Zero), due);
    }

    [Fact]
    public void Priority_change_recalculates_DueDate_from_original_CreatedDate()
    {
        // CreatedDate remains the anchor when priority changes Normal → Critical.
        var createdDate = new DateTimeOffset(2026, 9, 2, 10, 0, 0, TimeSpan.Zero);

        var dueWhileNormal = DueDateCalculator.Calculate(createdDate, Priority.Normal);
        Assert.Equal(new DateTimeOffset(2026, 9, 5, 10, 0, 0, TimeSpan.Zero), dueWhileNormal);

        var dueAfterChangeToCritical = DueDateCalculator.Calculate(createdDate, Priority.Critical);

        Assert.Equal(new DateTimeOffset(2026, 9, 2, 14, 0, 0, TimeSpan.Zero), dueAfterChangeToCritical);
    }
}
