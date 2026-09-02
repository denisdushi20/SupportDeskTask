using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Policies;

namespace SupportDesk.Infrastructure.Persistence.Seed;

public static class SupportDeskSeedData
{
    public static readonly Guid Agent1Id = Guid.Parse("11111111-1111-1111-1111-111111111101");
    public static readonly Guid Agent2Id = Guid.Parse("11111111-1111-1111-1111-111111111102");
    public static readonly Guid Agent3Id = Guid.Parse("11111111-1111-1111-1111-111111111103");
    public static readonly Guid Agent4Id = Guid.Parse("11111111-1111-1111-1111-111111111104");
    public static readonly Guid Agent5Id = Guid.Parse("11111111-1111-1111-1111-111111111105");

    public const int ExpectedAgentCount = 5;
    public const int ExpectedTicketCount = 20;
    public const int ExpectedSeedYear = 2026;
    public const int ExpectedSeedCounterValue = 20;

    private static readonly Guid[] ExpectedAgentIds =
    [
        Agent1Id, Agent2Id, Agent3Id, Agent4Id, Agent5Id
    ];

    private static readonly string[] ExpectedTicketReferences =
        Enumerable.Range(1, ExpectedTicketCount)
            .Select(n => $"TCK-2026-{n:D4}")
            .ToArray();

    // Fixed UTC anchor — never DateTime.Now
    private static readonly DateTimeOffset SeedDay =
        new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    public static async Task EnsureSeededAsync(
        SupportDeskDbContext db,
        CancellationToken cancellationToken = default)
    {
        // Readiness is based on the deterministic seed markers (fixed agent IDs,
        // TCK-2026-0001..0020, counter year/value), not "any agent exists".
        // Fully seeded (including DBs that later gained more tickets) → skip.
        // Completely empty → insert full dataset once.
        // Partial/manual data → fail clearly; do not duplicate or wipe.
        var readiness = await EvaluateSeedReadinessAsync(db, cancellationToken);
        if (readiness == SeedReadiness.FullySeeded)
        {
            return;
        }

        if (readiness == SeedReadiness.Partial)
        {
            throw new InvalidOperationException(
                "Support Desk database appears partially seeded or contains unexpected data. " +
                $"Expected either an empty database or the full seed ({ExpectedAgentCount} known agents, " +
                $"{ExpectedTicketCount} known ticket references TCK-2026-0001..0020, and counter " +
                $"{ExpectedSeedYear}/{ExpectedSeedCounterValue}+). " +
                "Refusing to seed automatically to avoid duplicates or overwriting existing data.");
        }

        var agents = CreateAgents();
        var tickets = CreateTickets();
        var comments = CreateComments(tickets);

        db.Agents.AddRange(agents);
        db.Tickets.AddRange(tickets);
        db.Comments.AddRange(comments);

        db.TicketReferenceCounters.Add(new TicketReferenceCounter
        {
            Year = ExpectedSeedYear,
            LastValue = ExpectedSeedCounterValue
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private enum SeedReadiness
    {
        Empty,
        FullySeeded,
        Partial
    }

    private static async Task<SeedReadiness> EvaluateSeedReadinessAsync(
        SupportDeskDbContext db,
        CancellationToken cancellationToken)
    {
        var agentCount = await db.Agents.CountAsync(cancellationToken);
        var ticketCount = await db.Tickets.CountAsync(cancellationToken);
        var commentCount = await db.Comments.CountAsync(cancellationToken);
        var counterCount = await db.TicketReferenceCounters.CountAsync(cancellationToken);

        if (agentCount == 0 && ticketCount == 0 && commentCount == 0 && counterCount == 0)
        {
            return SeedReadiness.Empty;
        }

        var seedAgentCount = await db.Agents
            .CountAsync(a => ExpectedAgentIds.Contains(a.Id), cancellationToken);

        var seedTicketCount = await db.Tickets
            .CountAsync(t => ExpectedTicketReferences.Contains(t.Reference), cancellationToken);

        var counter = await db.TicketReferenceCounters
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Year == ExpectedSeedYear, cancellationToken);

        var counterLooksSeeded = counter is not null && counter.LastValue >= ExpectedSeedCounterValue;

        if (seedAgentCount == ExpectedAgentCount
            && seedTicketCount == ExpectedTicketCount
            && counterLooksSeeded)
        {
            return SeedReadiness.FullySeeded;
        }

        return SeedReadiness.Partial;
    }

    private static List<Agent> CreateAgents() =>
    [
        new()
        {
            Id = Agent1Id,
            FullName = "Alex Technical",
            Email = "alex.technical@supportdesk.local",
            Department = Department.Technical,
            Active = true
        },
        new()
        {
            Id = Agent2Id,
            FullName = "Blair Billing",
            Email = "blair.billing@supportdesk.local",
            Department = Department.Billing,
            Active = true
        },
        new()
        {
            Id = Agent3Id,
            FullName = "Casey General",
            Email = "casey.general@supportdesk.local",
            Department = Department.General,
            Active = true
        },
        new()
        {
            Id = Agent4Id,
            FullName = "Dana Technical",
            Email = "dana.technical@supportdesk.local",
            Department = Department.Technical,
            Active = true
        },
        new()
        {
            Id = Agent5Id,
            FullName = "Evan Inactive",
            Email = "evan.inactive@supportdesk.local",
            Department = Department.Billing,
            Active = false
        }
    ];

    private static List<Ticket> CreateTickets()
    {
        var tickets = new List<Ticket>(20);

        void Add(
            int number,
            string title,
            Priority priority,
            Status status,
            Guid? agentId,
            int createdDayOffsetHours,
            DateTimeOffset? resolvedDate,
            DateTimeOffset? closedDate,
            bool forceOverdue)
        {
            var created = SeedDay.AddHours(createdDayOffsetHours);
            var due = DueDateCalculator.Calculate(created, priority);

            if (forceOverdue)
            {
                // Keep calculator-derived offset shape but ensure DueDate is in the past relative to SeedDay+30d evaluation window.
                // Absolute past due: created earlier in August 2026 so due is before SeedDay.
                created = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero).AddHours(number);
                due = DueDateCalculator.Calculate(created, priority);
            }

            tickets.Add(new Ticket
            {
                Id = Guid.Parse($"22222222-2222-2222-2222-2222222200{number:D2}"),
                Reference = $"TCK-2026-{number:D4}",
                Title = title,
                Description = $"Seed description for {title}",
                CustomerName = $"Customer {number}",
                CustomerEmail = $"customer{number}@example.com",
                Priority = priority,
                Status = status,
                AssignedAgentId = agentId,
                CreatedDate = created,
                LastModifiedDate = closedDate ?? resolvedDate ?? created.AddHours(2),
                ResolvedDate = resolvedDate,
                ClosedDate = closedDate,
                DueDate = due
            });
        }

        // 1-3: New, mix priorities, unassigned / assigned (including overdue)
        Add(1, "Cannot login", Priority.High, Status.New, null, 0, null, null, forceOverdue: false);
        Add(2, "Invoice mismatch", Priority.Normal, Status.New, Agent2Id, 1, null, null, forceOverdue: false);
        Add(3, "Overdue critical outage", Priority.Critical, Status.New, Agent1Id, 0, null, null, forceOverdue: true);

        // 4-7: InProgress (one reopened: ResolvedDate null)
        Add(4, "VPN intermittent", Priority.High, Status.InProgress, Agent1Id, 2, null, null, forceOverdue: false);
        Add(5, "Password reset flow", Priority.Normal, Status.InProgress, Agent3Id, 3, null, null, forceOverdue: false);
        Add(6, "Reopened billing dispute", Priority.High, Status.InProgress, Agent2Id, 4,
            resolvedDate: null, closedDate: null, forceOverdue: false);
        Add(7, "Overdue in-progress printer", Priority.Low, Status.InProgress, Agent4Id, 0, null, null, forceOverdue: true);

        // 8-10: still assigned to inactive agent (no auto-unassign)
        Add(8, "Legacy billing export", Priority.Normal, Status.InProgress, Agent5Id, 5, null, null, forceOverdue: false);
        Add(9, "Inactive agent follow-up", Priority.Low, Status.New, Agent5Id, 6, null, null, forceOverdue: false);
        Add(10, "Overdue on inactive agent", Priority.Normal, Status.New, Agent5Id, 0, null, null, forceOverdue: true);

        // 11-14: Resolved
        var resolvedAt = SeedDay.AddDays(2);
        Add(11, "Email sync fixed", Priority.Normal, Status.Resolved, Agent1Id, 7, resolvedAt, null, forceOverdue: false);
        Add(12, "Refund processed", Priority.High, Status.Resolved, Agent2Id, 8, resolvedAt.AddHours(1), null, forceOverdue: false);
        Add(13, "FAQ updated", Priority.Low, Status.Resolved, Agent3Id, 9, resolvedAt.AddHours(2), null, forceOverdue: false);
        Add(14, "Critical patch applied", Priority.Critical, Status.Resolved, Agent4Id, 10, resolvedAt.AddHours(3), null, forceOverdue: false);

        // 15-18: Closed
        var closedAt = SeedDay.AddDays(5);
        Add(15, "Closed onboarding issue", Priority.Normal, Status.Closed, Agent1Id, 11, closedAt.AddDays(-1), closedAt, forceOverdue: false);
        Add(16, "Closed invoice copy", Priority.Low, Status.Closed, Agent2Id, 12, closedAt.AddDays(-1), closedAt.AddHours(1), forceOverdue: false);
        Add(17, "Closed general inquiry", Priority.Normal, Status.Closed, Agent3Id, 13, closedAt.AddDays(-1), closedAt.AddHours(2), forceOverdue: false);
        Add(18, "Closed high priority outage", Priority.High, Status.Closed, Agent4Id, 14, closedAt.AddDays(-1), closedAt.AddHours(3), forceOverdue: false);

        // 19-20: unassigned New / Critical open
        Add(19, "Unassigned feature question", Priority.Low, Status.New, null, 15, null, null, forceOverdue: false);
        Add(20, "Unassigned critical access", Priority.Critical, Status.New, null, 16, null, null, forceOverdue: false);

        return tickets;
    }

    private static List<Comment> CreateComments(IReadOnlyList<Ticket> tickets)
    {
        Comment C(int n, Guid ticketId, string author, string body, DateTimeOffset at) => new()
        {
            Id = Guid.Parse($"33333333-3333-3333-3333-3333333300{n:D2}"),
            TicketId = ticketId,
            AuthorName = author,
            Body = body,
            CreatedDate = at
        };

        var t4 = tickets.Single(t => t.Reference == "TCK-2026-0004").Id;
        var t6 = tickets.Single(t => t.Reference == "TCK-2026-0006").Id;
        var t11 = tickets.Single(t => t.Reference == "TCK-2026-0011").Id;
        var t15 = tickets.Single(t => t.Reference == "TCK-2026-0015").Id;
        var t3 = tickets.Single(t => t.Reference == "TCK-2026-0003").Id;

        return
        [
            C(1, t4, "Alex Technical", "Investigating VPN logs.", SeedDay.AddHours(3)),
            C(2, t4, "Customer 4", "Still dropping every hour.", SeedDay.AddHours(4)),
            C(3, t6, "Blair Billing", "Reopened after customer reply.", SeedDay.AddHours(5)),
            C(4, t11, "Alex Technical", "Sync completed successfully.", SeedDay.AddDays(2)),
            C(5, t15, "Casey General", "Closing after confirmation.", SeedDay.AddDays(5)),
            C(6, t3, "Dana Technical", "Critical path monitoring.", SeedDay.AddHours(2)),
            C(7, tickets.Single(t => t.Reference == "TCK-2026-0008").Id, "Evan Inactive", "Historical note before leave.", SeedDay.AddHours(6))
        ];
    }
}
