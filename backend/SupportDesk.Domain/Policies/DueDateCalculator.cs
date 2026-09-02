using SupportDesk.Domain.Enums;

namespace SupportDesk.Domain.Policies;

/// <summary>
/// Pure due-date calculation from CreatedDate + Priority.
/// Independent of current time.
/// </summary>
public static class DueDateCalculator
{
    public static DateTimeOffset Calculate(DateTimeOffset createdDate, Priority priority) =>
        priority switch
        {
            Priority.Critical => createdDate.AddHours(4),
            Priority.High => createdDate.AddDays(1),
            Priority.Normal => createdDate.AddDays(3),
            Priority.Low => createdDate.AddDays(7),
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown priority.")
        };
}
