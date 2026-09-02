using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Policies;
using SupportDesk.Infrastructure.Persistence;
using SupportDesk.Infrastructure.Persistence.Seed;

namespace SupportDesk.Tests.Persistence;

public class TicketReferenceGeneratorTests
{
    private static SupportDeskDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<SupportDeskDbContext>()
            .UseSqlServer(
                $"Server=localhost;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true")
            .Options;

        var db = new SupportDeskDbContext(options);
        db.Database.EnsureDeleted();
        db.Database.Migrate();
        return db;
    }

    [Fact]
    public async Task First_ticket_of_year_gets_0001_and_counter_starts_at_1()
    {
        await using var db = CreateContext("SupportDesk_RefGen_First_" + Guid.NewGuid().ToString("N"));
        var generator = new SqlServerTicketReferenceGenerator(db);
        var created = new DateTimeOffset(2026, 9, 2, 10, 15, 0, TimeSpan.Zero);

        await using var tx = await db.Database.BeginTransactionAsync();
        var reference = await generator.AllocateNextAsync(created);

        db.Tickets.Add(CreateMinimalTicket(reference, created));
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        Assert.Equal("TCK-2026-0001", reference);
        var counter = await db.TicketReferenceCounters.SingleAsync(c => c.Year == 2026);
        Assert.Equal(1, counter.LastValue);
    }

    [Fact]
    public async Task Existing_counter_increments_and_next_after_seed_is_0021()
    {
        await using var db = CreateContext("SupportDesk_RefGen_Next_" + Guid.NewGuid().ToString("N"));
        await SupportDeskSeedData.EnsureSeededAsync(db);

        var counterBefore = await db.TicketReferenceCounters.SingleAsync(c => c.Year == 2026);
        Assert.Equal(20, counterBefore.LastValue);
        Assert.Equal(20, await db.Tickets.CountAsync());

        var generator = new SqlServerTicketReferenceGenerator(db);
        var created = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

        await using var tx = await db.Database.BeginTransactionAsync();
        var reference = await generator.AllocateNextAsync(created);
        db.Tickets.Add(CreateMinimalTicket(reference, created));
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        Assert.Equal("TCK-2026-0021", reference);
        Assert.Equal(21, await db.TicketReferenceCounters.Where(c => c.Year == 2026).Select(c => c.LastValue).SingleAsync());
    }

    [Fact]
    public async Task Allocate_without_ambient_transaction_throws()
    {
        await using var db = CreateContext("SupportDesk_RefGen_NoTx_" + Guid.NewGuid().ToString("N"));
        var generator = new SqlServerTicketReferenceGenerator(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            generator.AllocateNextAsync(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task Concurrent_first_allocations_produce_distinct_references()
    {
        var databaseName = "SupportDesk_RefGen_Race_" + Guid.NewGuid().ToString("N");
        await using (var setup = CreateContext(databaseName))
        {
            // schema only
        }

        var created = new DateTimeOffset(2027, 3, 1, 9, 0, 0, TimeSpan.Zero);

        async Task<string> AllocateOneAsync()
        {
            var options = new DbContextOptionsBuilder<SupportDeskDbContext>()
                .UseSqlServer(
                    $"Server=localhost;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true")
                .Options;

            await using var db = new SupportDeskDbContext(options);
            var generator = new SqlServerTicketReferenceGenerator(db);

            await using var tx = await db.Database.BeginTransactionAsync();
            var reference = await generator.AllocateNextAsync(created);
            db.Tickets.Add(CreateMinimalTicket(reference, created));
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return reference;
        }

        var tasks = Enumerable.Range(0, 8).Select(_ => AllocateOneAsync());
        var references = await Task.WhenAll(tasks);

        Assert.Equal(8, references.Distinct().Count());
        Assert.Contains("TCK-2027-0001", references);
        Assert.Equal(8, references.Select(r => int.Parse(r[^4..])).Max());
    }

    [Fact]
    public async Task Seed_is_idempotent_and_sets_counter_to_20()
    {
        await using var db = CreateContext("SupportDesk_Seed_" + Guid.NewGuid().ToString("N"));

        await SupportDeskSeedData.EnsureSeededAsync(db);
        await SupportDeskSeedData.EnsureSeededAsync(db);

        Assert.Equal(5, await db.Agents.CountAsync());
        Assert.Equal(20, await db.Tickets.CountAsync());
        Assert.True(await db.Comments.CountAsync() >= 7);
        Assert.Equal(20, await db.TicketReferenceCounters.Where(c => c.Year == 2026).Select(c => c.LastValue).SingleAsync());
        Assert.Contains(await db.Agents.ToListAsync(), a => !a.Active);
        Assert.Contains(await db.Tickets.ToListAsync(), t => t.Status == Status.InProgress && t.ResolvedDate is null);
        Assert.Contains(await db.Tickets.ToListAsync(), t => t.AssignedAgentId is null);
        Assert.Contains(await db.Tickets.ToListAsync(), t => t.Status == Status.Closed);
    }

    [Fact]
    public async Task Allocate_then_rollback_does_not_permanently_advance_counter()
    {
        await using var db = CreateContext("SupportDesk_RefGen_Rollback_" + Guid.NewGuid().ToString("N"));
        await SupportDeskSeedData.EnsureSeededAsync(db);

        Assert.Equal(20, await db.TicketReferenceCounters.Where(c => c.Year == 2026).Select(c => c.LastValue).SingleAsync());

        var generator = new SqlServerTicketReferenceGenerator(db);
        var created = new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var reference = await generator.AllocateNextAsync(created);
            Assert.Equal("TCK-2026-0021", reference);

            // Roll back without inserting/committing a Ticket.
            await tx.RollbackAsync();
        }

        // Clear tracker so we re-read committed state from SQL Server.
        db.ChangeTracker.Clear();

        Assert.Equal(20, await db.TicketReferenceCounters.Where(c => c.Year == 2026).Select(c => c.LastValue).SingleAsync());
        Assert.Equal(20, await db.Tickets.CountAsync());

        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            var referenceAgain = await generator.AllocateNextAsync(created);
            Assert.Equal("TCK-2026-0021", referenceAgain);
            await tx.RollbackAsync();
        }

        db.ChangeTracker.Clear();
        Assert.Equal(20, await db.TicketReferenceCounters.Where(c => c.Year == 2026).Select(c => c.LastValue).SingleAsync());
    }

    [Fact]
    public async Task Partial_seed_state_fails_clearly_instead_of_skipping()
    {
        await using var db = CreateContext("SupportDesk_Seed_Partial_" + Guid.NewGuid().ToString("N"));

        db.Agents.Add(new Agent
        {
            Id = Guid.NewGuid(),
            FullName = "Orphan Agent",
            Email = "orphan@supportdesk.local",
            Department = Department.General,
            Active = true
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => SupportDeskSeedData.EnsureSeededAsync(db));
        Assert.Contains("partially seeded", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, await db.Agents.CountAsync());
        Assert.Equal(0, await db.Tickets.CountAsync());
    }

    private static Ticket CreateMinimalTicket(string reference, DateTimeOffset created) =>
        new()
        {
            Id = Guid.NewGuid(),
            Reference = reference,
            Title = "Generated",
            Description = "Generated ticket",
            CustomerName = "Cust",
            CustomerEmail = "cust@example.com",
            Priority = Priority.Normal,
            Status = Status.New,
            CreatedDate = created,
            LastModifiedDate = created,
            DueDate = DueDateCalculator.Calculate(created, Priority.Normal)
        };
}
