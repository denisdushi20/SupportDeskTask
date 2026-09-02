using Microsoft.EntityFrameworkCore;

namespace SupportDesk.Infrastructure.Persistence;

public sealed class SqlServerTicketReferenceGenerator : ITicketReferenceGenerator
{
    private readonly SupportDeskDbContext _db;

    public SqlServerTicketReferenceGenerator(SupportDeskDbContext db)
    {
        _db = db;
    }

    public async Task<string> AllocateNextAsync(
        DateTimeOffset createdDate,
        CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "AllocateNextAsync requires an ambient database transaction so the counter update and ticket insert commit atomically.");
        }

        var year = createdDate.UtcDateTime.Year;
        const int maxAttempts = 5;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var counter = await _db.TicketReferenceCounters
                .FromSqlRaw(
                    """
                    SELECT [Year], [LastValue]
                    FROM [TicketReferenceCounters] WITH (UPDLOCK, ROWLOCK, HOLDLOCK)
                    WHERE [Year] = {0}
                    """,
                    year)
                .AsTracking()
                .SingleOrDefaultAsync(cancellationToken);

            if (counter is null)
            {
                counter = new TicketReferenceCounter
                {
                    Year = year,
                    LastValue = 1
                };

                _db.TicketReferenceCounters.Add(counter);

                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                    return Format(year, 1);
                }
                catch (DbUpdateException)
                {
                    _db.Entry(counter).State = EntityState.Detached;
                    continue;
                }
            }

            counter.LastValue++;
            await _db.SaveChangesAsync(cancellationToken);
            return Format(year, counter.LastValue);
        }

        throw new InvalidOperationException(
            $"Failed to allocate a ticket reference for year {year} after concurrent insert retries.");
    }

    private static string Format(int year, int sequence) =>
        $"TCK-{year}-{sequence:D4}";
}
