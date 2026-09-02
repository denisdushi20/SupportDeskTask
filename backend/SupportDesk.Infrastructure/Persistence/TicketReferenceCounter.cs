namespace SupportDesk.Infrastructure.Persistence;

/// <summary>
/// Per-year monotonic counter for ticket references (TCK-YYYY-NNNN).
/// Persistence-only; not part of the Domain model.
/// </summary>
public class TicketReferenceCounter
{
    public int Year { get; set; }

    public int LastValue { get; set; }
}
