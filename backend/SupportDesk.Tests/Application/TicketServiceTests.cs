using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Application.Common;
using SupportDesk.Api.Application.Tickets;
using SupportDesk.Api.Contracts;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Policies;
using SupportDesk.Infrastructure.Persistence;
using SupportDesk.Infrastructure.Persistence.Seed;
using SupportDesk.Tests.TestSupport;

namespace SupportDesk.Tests.Application;

public class TicketServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 9, 2, 15, 30, 0, TimeSpan.Zero);

    private sealed class Sut : IAsyncDisposable
    {
        public required SupportDeskDbContext Db { get; init; }
        public required TicketService Service { get; init; }
        public required FakeClock Clock { get; init; }

        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private static async Task<Sut> CreateSutAsync(bool seed = true)
    {
        var cs = SqlServerTestDatabase.CreateConnectionString("SupportDesk_AppSvc");
        var db = SqlServerTestDatabase.CreateContext(cs);
        if (seed)
        {
            await SupportDeskSeedData.EnsureSeededAsync(db);
        }

        var clock = new FakeClock(FixedNow);
        var service = new TicketService(db, new SqlServerTicketReferenceGenerator(db), clock);
        return new Sut { Db = db, Service = service, Clock = clock };
    }

    private static CreateTicketRequest ValidCreate(Priority priority = Priority.High) => new()
    {
        Title = "Cannot login",
        Description = "User cannot login to portal",
        CustomerName = "Pat Customer",
        CustomerEmail = "pat@example.com",
        Priority = priority
    };

    [Fact]
    public async Task Create_sets_New_status_unassigned_DueDate_and_reference()
    {
        await using var sut = await CreateSutAsync(seed: false);

        var result = await sut.Service.CreateAsync(ValidCreate(Priority.Critical));

        Assert.True(result.IsSuccess);
        var ticket = result.Value!;
        Assert.Equal(Status.New, ticket.Status);
        Assert.Null(ticket.AssignedAgentId);
        Assert.Equal(FixedNow, ticket.CreatedDate);
        Assert.Equal(FixedNow, ticket.LastModifiedDate);
        Assert.Equal(DueDateCalculator.Calculate(FixedNow, Priority.Critical), ticket.DueDate);
        Assert.Equal("TCK-2026-0001", ticket.Reference);
        Assert.Equal(
            1,
            (await sut.Db.TicketReferenceCounters.SingleAsync(c => c.Year == 2026)).LastValue);
    }

    [Fact]
    public async Task Create_after_seed_allocates_next_reference_atomically()
    {
        await using var sut = await CreateSutAsync(seed: true);

        var result = await sut.Service.CreateAsync(ValidCreate());

        Assert.True(result.IsSuccess);
        Assert.Equal("TCK-2026-0021", result.Value!.Reference);
        Assert.Equal(
            21,
            (await sut.Db.TicketReferenceCounters.SingleAsync(c => c.Year == 2026)).LastValue);
    }

    [Fact]
    public async Task Update_allows_New_and_InProgress_rejects_Resolved_and_Closed()
    {
        await using var sut = await CreateSutAsync();

        var request = new UpdateTicketRequest
        {
            Title = "Updated",
            Description = "Updated desc",
            CustomerName = "Updated Customer",
            CustomerEmail = "updated@example.com",
            Priority = Priority.Low
        };

        Assert.True((await sut.Service.UpdateAsync(
            Guid.Parse("22222222-2222-2222-2222-222222220001"), request)).IsSuccess);
        Assert.True((await sut.Service.UpdateAsync(
            Guid.Parse("22222222-2222-2222-2222-222222220004"), request)).IsSuccess);

        var resolved = await sut.Service.UpdateAsync(
            Guid.Parse("22222222-2222-2222-2222-222222220011"), request);
        Assert.Equal(AppErrorCodes.TicketNotEditable, resolved.Error!.Code);

        var closed = await sut.Service.UpdateAsync(
            Guid.Parse("22222222-2222-2222-2222-222222220015"), request);
        Assert.Equal(AppErrorCodes.TicketClosed, closed.Error!.Code);
    }

    [Fact]
    public async Task Update_priority_recalculates_DueDate_from_CreatedDate_not_now()
    {
        await using var sut = await CreateSutAsync();

        var ticketId = Guid.Parse("22222222-2222-2222-2222-222222220001");
        var ticket = await sut.Db.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticketId);
        var created = ticket.CreatedDate;
        sut.Clock.UtcNow = FixedNow.AddDays(10);

        var result = await sut.Service.UpdateAsync(ticketId, new UpdateTicketRequest
        {
            Title = ticket.Title,
            Description = ticket.Description,
            CustomerName = ticket.CustomerName,
            CustomerEmail = ticket.CustomerEmail,
            Priority = Priority.Critical
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(DueDateCalculator.Calculate(created, Priority.Critical), result.Value!.DueDate);
        Assert.NotEqual(
            DueDateCalculator.Calculate(sut.Clock.UtcNow, Priority.Critical),
            result.Value.DueDate);
    }

    [Fact]
    public async Task Update_noop_does_not_bump_LastModifiedDate()
    {
        await using var sut = await CreateSutAsync();

        var ticketId = Guid.Parse("22222222-2222-2222-2222-222222220001");
        var before = await sut.Db.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticketId);
        sut.Clock.UtcNow = FixedNow.AddHours(5);

        var result = await sut.Service.UpdateAsync(ticketId, new UpdateTicketRequest
        {
            Title = before.Title,
            Description = before.Description,
            CustomerName = before.CustomerName,
            CustomerEmail = before.CustomerEmail,
            Priority = before.Priority
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(before.LastModifiedDate, result.Value!.LastModifiedDate);
    }

    [Theory]
    [InlineData("Title")]
    [InlineData("Description")]
    [InlineData("CustomerName")]
    [InlineData("CustomerEmail")]
    public async Task Update_whitespace_only_fields_return_VALIDATION_ERROR(string field)
    {
        await using var sut = await CreateSutAsync();

        var ticketId = Guid.Parse("22222222-2222-2222-2222-222222220001");
        var before = await sut.Db.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticketId);

        var request = new UpdateTicketRequest
        {
            Title = before.Title,
            Description = before.Description,
            CustomerName = before.CustomerName,
            CustomerEmail = before.CustomerEmail,
            Priority = before.Priority
        };

        switch (field)
        {
            case "Title":
                request.Title = "   ";
                break;
            case "Description":
                request.Description = "   ";
                break;
            case "CustomerName":
                request.CustomerName = "   ";
                break;
            case "CustomerEmail":
                request.CustomerEmail = "   ";
                break;
        }

        var result = await sut.Service.UpdateAsync(ticketId, request);

        Assert.True(result.IsFailure);
        Assert.Equal(AppErrorCodes.ValidationError, result.Error!.Code);

        var after = await sut.Db.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticketId);
        Assert.Equal(before.Title, after.Title);
        Assert.Equal(before.LastModifiedDate, after.LastModifiedDate);
    }

    [Fact]
    public async Task Assign_active_succeeds_inactive_missing_and_closed_fail_reassignment_works()
    {
        await using var sut = await CreateSutAsync();

        var newUnassigned = Guid.Parse("22222222-2222-2222-2222-222222220001");
        var closed = Guid.Parse("22222222-2222-2222-2222-222222220015");

        var ok = await sut.Service.AssignAsync(newUnassigned, SupportDeskSeedData.Agent1Id);
        Assert.True(ok.IsSuccess);
        Assert.Equal(SupportDeskSeedData.Agent1Id, ok.Value!.AssignedAgentId);

        var reassign = await sut.Service.AssignAsync(newUnassigned, SupportDeskSeedData.Agent2Id);
        Assert.True(reassign.IsSuccess);
        Assert.Equal(SupportDeskSeedData.Agent2Id, reassign.Value!.AssignedAgentId);

        var inactive = await sut.Service.AssignAsync(newUnassigned, SupportDeskSeedData.Agent5Id);
        Assert.Equal(AppErrorCodes.AgentInactive, inactive.Error!.Code);

        var missing = await sut.Service.AssignAsync(newUnassigned, Guid.NewGuid());
        Assert.Equal(AppErrorCodes.AgentNotFound, missing.Error!.Code);

        var closedAssign = await sut.Service.AssignAsync(closed, SupportDeskSeedData.Agent1Id);
        Assert.Equal(AppErrorCodes.TicketClosed, closedAssign.Error!.Code);
    }

    [Fact]
    public async Task Unassign_works_and_already_unassigned_is_noop()
    {
        await using var sut = await CreateSutAsync();

        var unassignedId = Guid.Parse("22222222-2222-2222-2222-222222220019");
        var before = await sut.Db.Tickets.AsNoTracking().SingleAsync(t => t.Id == unassignedId);
        sut.Clock.UtcNow = FixedNow.AddHours(3);

        var noop = await sut.Service.UnassignAsync(unassignedId);
        Assert.True(noop.IsSuccess);
        Assert.Equal(before.LastModifiedDate, noop.Value!.LastModifiedDate);

        var assignedId = Guid.Parse("22222222-2222-2222-2222-222222220002");
        var unassign = await sut.Service.UnassignAsync(assignedId);
        Assert.True(unassign.IsSuccess);
        Assert.Null(unassign.Value!.AssignedAgentId);
        Assert.Equal(sut.Clock.UtcNow, unassign.Value.LastModifiedDate);

        var closed = await sut.Service.UnassignAsync(Guid.Parse("22222222-2222-2222-2222-222222220015"));
        Assert.Equal(AppErrorCodes.TicketClosed, closed.Error!.Code);
    }

    [Fact]
    public async Task Existing_inactive_assignment_is_not_auto_unassigned_on_get()
    {
        await using var sut = await CreateSutAsync();

        var result = await sut.Service.GetByIdAsync(Guid.Parse("22222222-2222-2222-2222-222222220008"));

        Assert.True(result.IsSuccess);
        Assert.Equal(SupportDeskSeedData.Agent5Id, result.Value!.AssignedAgentId);
        Assert.False(result.Value.AssignedAgent!.Active);
    }

    [Fact]
    public async Task ChangeStatus_happy_paths_and_resolve_guards()
    {
        await using var sut = await CreateSutAsync(seed: false);

        sut.Db.Agents.AddRange(
            new Agent
            {
                Id = SupportDeskSeedData.Agent1Id,
                FullName = "Active",
                Email = "active@test.local",
                Department = Department.Technical,
                Active = true
            },
            new Agent
            {
                Id = SupportDeskSeedData.Agent5Id,
                FullName = "Inactive",
                Email = "inactive@test.local",
                Department = Department.Billing,
                Active = false
            });
        await sut.Db.SaveChangesAsync();

        var create = await sut.Service.CreateAsync(ValidCreate());
        var id = create.Value!.Id;

        Assert.True((await sut.Service.ChangeStatusAsync(id, Status.InProgress)).IsSuccess);

        var resolveNoAgent = await sut.Service.ChangeStatusAsync(id, Status.Resolved);
        Assert.Equal(AppErrorCodes.AssignmentRequired, resolveNoAgent.Error!.Code);

        var assignInactive = await sut.Service.AssignAsync(id, SupportDeskSeedData.Agent5Id);
        Assert.Equal(AppErrorCodes.AgentInactive, assignInactive.Error!.Code);

        Assert.True((await sut.Service.AssignAsync(id, SupportDeskSeedData.Agent1Id)).IsSuccess);
        var resolveOk = await sut.Service.ChangeStatusAsync(id, Status.Resolved);
        Assert.True(resolveOk.IsSuccess);
        Assert.Equal(Status.Resolved, resolveOk.Value!.Status);
        Assert.Equal(sut.Clock.UtcNow, resolveOk.Value.ResolvedDate);

        var reopen = await sut.Service.ChangeStatusAsync(id, Status.InProgress);
        Assert.True(reopen.IsSuccess);
        Assert.Null(reopen.Value!.ResolvedDate);
        Assert.Null(reopen.Value.ClosedDate);

        sut.Clock.UtcNow = FixedNow.AddMinutes(1);
        Assert.True((await sut.Service.ChangeStatusAsync(id, Status.Resolved)).IsSuccess);
        var close = await sut.Service.ChangeStatusAsync(id, Status.Closed);
        Assert.True(close.IsSuccess);
        Assert.Equal(Status.Closed, close.Value!.Status);
        Assert.NotNull(close.Value.ClosedDate);

        var closedAgain = await sut.Service.ChangeStatusAsync(id, Status.InProgress);
        Assert.Equal(AppErrorCodes.TicketClosed, closedAgain.Error!.Code);

        var create2 = await sut.Service.CreateAsync(ValidCreate(Priority.Normal));
        var id2 = create2.Value!.Id;
        Assert.True((await sut.Service.ChangeStatusAsync(id2, Status.InProgress)).IsSuccess);
        Assert.True((await sut.Service.AssignAsync(id2, SupportDeskSeedData.Agent1Id)).IsSuccess);

        var agent = await sut.Db.Agents.SingleAsync(a => a.Id == SupportDeskSeedData.Agent1Id);
        agent.Active = false;
        await sut.Db.SaveChangesAsync();

        var resolveInactive = await sut.Service.ChangeStatusAsync(id2, Status.Resolved);
        Assert.Equal(AppErrorCodes.AgentInactive, resolveInactive.Error!.Code);

        var invalid = await sut.Service.ChangeStatusAsync(id2, Status.Closed);
        Assert.Equal(AppErrorCodes.InvalidStatusTransition, invalid.Error!.Code);
        Assert.Equal("InProgress", invalid.Error.Context!["currentStatus"]);
        Assert.Equal("Closed", invalid.Error.Context["requestedStatus"]);
    }

    [Fact]
    public async Task Comments_allowed_on_open_and_resolved_rejected_on_closed_share_timestamp()
    {
        await using var sut = await CreateSutAsync();
        sut.Clock.UtcNow = FixedNow;

        var openId = Guid.Parse("22222222-2222-2222-2222-222222220004");
        var resolvedId = Guid.Parse("22222222-2222-2222-2222-222222220011");
        var closedId = Guid.Parse("22222222-2222-2222-2222-222222220015");

        var open = await sut.Service.AddCommentAsync(openId, new CreateCommentRequest
        {
            AuthorName = "Tester",
            Body = "Looking into it"
        });
        Assert.True(open.IsSuccess);
        Assert.Equal(FixedNow, open.Value!.CreatedDate);

        var detail = await sut.Service.GetByIdAsync(openId);
        Assert.Equal(FixedNow, detail.Value!.LastModifiedDate);

        Assert.True((await sut.Service.AddCommentAsync(resolvedId, new CreateCommentRequest
        {
            AuthorName = "Tester",
            Body = "Follow-up"
        })).IsSuccess);

        var closed = await sut.Service.AddCommentAsync(closedId, new CreateCommentRequest
        {
            AuthorName = "Tester",
            Body = "Nope"
        });
        Assert.Equal(AppErrorCodes.TicketClosed, closed.Error!.Code);
    }

    [Fact]
    public async Task Delete_non_closed_cascades_comments_closed_rejected()
    {
        await using var sut = await CreateSutAsync();

        var openId = Guid.Parse("22222222-2222-2222-2222-222222220004");
        Assert.True(await sut.Db.Comments.AnyAsync(c => c.TicketId == openId));

        Assert.True((await sut.Service.DeleteAsync(openId)).IsSuccess);
        Assert.False(await sut.Db.Tickets.AnyAsync(t => t.Id == openId));
        Assert.False(await sut.Db.Comments.AnyAsync(c => c.TicketId == openId));

        var closed = await sut.Service.DeleteAsync(Guid.Parse("22222222-2222-2222-2222-222222220015"));
        Assert.Equal(AppErrorCodes.TicketClosed, closed.Error!.Code);
    }

    [Fact]
    public async Task List_filters_paginates_and_overdueOnly()
    {
        await using var sut = await CreateSutAsync();
        sut.Clock.UtcNow = FixedNow;

        var overdue = await sut.Service.ListAsync(new TicketListQuery
        {
            Page = 1,
            PageSize = 50,
            OverdueOnly = true
        });

        Assert.True(overdue.IsSuccess);
        Assert.All(overdue.Value!.Items, i => Assert.True(i.IsOverdue));
        Assert.Equal(overdue.Value.Items.Count, overdue.Value.TotalCount);

        var page = await sut.Service.ListAsync(new TicketListQuery { Page = 1, PageSize = 5 });
        Assert.Equal(5, page.Value!.Items.Count);
        Assert.Equal(20, page.Value.TotalCount);

        var byStatus = await sut.Service.ListAsync(new TicketListQuery { Status = Status.Closed, PageSize = 50 });
        Assert.All(byStatus.Value!.Items, i => Assert.Equal(Status.Closed, i.Status));

        var search = await sut.Service.ListAsync(new TicketListQuery { Search = "TCK-2026-0003", PageSize = 10 });
        Assert.Contains(search.Value!.Items, i => i.Reference == "TCK-2026-0003");

        var byAgent = await sut.Service.ListAsync(new TicketListQuery
        {
            AssignedAgentId = SupportDeskSeedData.Agent1Id,
            PageSize = 50
        });
        Assert.All(byAgent.Value!.Items, i => Assert.Equal(SupportDeskSeedData.Agent1Id, i.AssignedAgentId));
    }

    [Fact]
    public async Task GetById_missing_and_AllowedTransitions_respect_active_assignee()
    {
        await using var sut = await CreateSutAsync();

        var missing = await sut.Service.GetByIdAsync(Guid.NewGuid());
        Assert.Equal(AppErrorCodes.TicketNotFound, missing.Error!.Code);

        var inactiveAssigned = await sut.Service.GetByIdAsync(
            Guid.Parse("22222222-2222-2222-2222-222222220008"));
        Assert.DoesNotContain(Status.Resolved, inactiveAssigned.Value!.AllowedTransitions);

        var activeInProgress = await sut.Service.GetByIdAsync(
            Guid.Parse("22222222-2222-2222-2222-222222220004"));
        Assert.Contains(Status.Resolved, activeInProgress.Value!.AllowedTransitions);
    }
}
