namespace SupportDesk.Domain.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
