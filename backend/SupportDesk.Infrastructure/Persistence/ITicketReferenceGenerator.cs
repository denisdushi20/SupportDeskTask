namespace SupportDesk.Infrastructure.Persistence;

/// <summary>
/// Allocates the next TCK-YYYY-NNNN reference.
/// Must be called inside an ambient DbContext transaction shared with ticket insertion.
/// </summary>
public interface ITicketReferenceGenerator
{
    Task<string> AllocateNextAsync(DateTimeOffset createdDate, CancellationToken cancellationToken = default);
}
