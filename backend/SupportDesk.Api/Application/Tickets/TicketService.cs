using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Application.Common;
using SupportDesk.Api.Application.Mapping;
using SupportDesk.Api.Contracts;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Policies;
using SupportDesk.Domain.Time;
using SupportDesk.Infrastructure.Persistence;

namespace SupportDesk.Api.Application.Tickets;

public sealed class TicketService
{
    private readonly SupportDeskDbContext _db;
    private readonly ITicketReferenceGenerator _referenceGenerator;
    private readonly IClock _clock;

    public TicketService(
        SupportDeskDbContext db,
        ITicketReferenceGenerator referenceGenerator,
        IClock clock)
    {
        _db = db;
        _referenceGenerator = referenceGenerator;
        _clock = clock;
    }

    public async Task<AppResult<TicketDetailResponse>> CreateAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = request.Title.Trim();
        var description = request.Description.Trim();
        var customerName = request.CustomerName.Trim();
        var customerEmail = request.CustomerEmail.Trim();

        if (string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(description)
            || string.IsNullOrWhiteSpace(customerName)
            || string.IsNullOrWhiteSpace(customerEmail))
        {
            return AppResult<TicketDetailResponse>.Failure(
                AppErrorCodes.ValidationError,
                "Required ticket fields cannot be empty or whitespace.");
        }

        var priority = request.Priority
            ?? throw new InvalidOperationException("Priority must be validated before CreateAsync.");

        var now = _clock.UtcNow;
        var dueDate = DueDateCalculator.Calculate(now, priority);

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var reference = await _referenceGenerator.AllocateNextAsync(now, cancellationToken);

            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                Reference = reference,
                Title = title,
                Description = description,
                CustomerName = customerName,
                CustomerEmail = customerEmail,
                Priority = priority,
                Status = Status.New,
                AssignedAgentId = null,
                CreatedDate = now,
                LastModifiedDate = now,
                ResolvedDate = null,
                ClosedDate = null,
                DueDate = dueDate
            };

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return AppResult<TicketDetailResponse>.Success(
                TicketMapper.ToDetail(ticket, now, assignedAgentActiveForTransitions: null));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<AppResult<TicketDetailResponse>> GetByIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _db.Tickets
            .AsNoTracking()
            .Include(t => t.AssignedAgent)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            return TicketNotFound();
        }

        return AppResult<TicketDetailResponse>.Success(
            TicketMapper.ToDetail(ticket, _clock.UtcNow));
    }

    public async Task<AppResult<PagedResult<TicketListItemResponse>>> ListAsync(
        TicketListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);
        var utcNow = _clock.UtcNow;

        var tickets = _db.Tickets.AsNoTracking().AsQueryable();

        if (query.Status is { } status)
        {
            tickets = tickets.Where(t => t.Status == status);
        }

        if (query.Priority is { } priority)
        {
            tickets = tickets.Where(t => t.Priority == priority);
        }

        if (query.AssignedAgentId is { } agentId)
        {
            tickets = tickets.Where(t => t.AssignedAgentId == agentId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            tickets = tickets.Where(t =>
                t.Reference.Contains(term)
                || t.Title.Contains(term)
                || t.CustomerName.Contains(term));
        }

        if (query.OverdueOnly)
        {
            tickets = tickets.Where(t =>
                t.DueDate < utcNow
                && (t.Status == Status.New || t.Status == Status.InProgress));
        }

        var totalCount = await tickets.CountAsync(cancellationToken);

        var items = await tickets
            .OrderByDescending(t => t.CreatedDate)
            .ThenBy(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TicketListItemResponse
            {
                Id = t.Id,
                Reference = t.Reference,
                Title = t.Title,
                CustomerName = t.CustomerName,
                Priority = t.Priority,
                Status = t.Status,
                AssignedAgentId = t.AssignedAgentId,
                AssignedAgentName = t.AssignedAgent != null ? t.AssignedAgent.FullName : null,
                CreatedDate = t.CreatedDate,
                DueDate = t.DueDate,
                LastModifiedDate = t.LastModifiedDate,
                IsOverdue = t.DueDate < utcNow
                    && (t.Status == Status.New || t.Status == Status.InProgress)
            })
            .ToListAsync(cancellationToken);

        return AppResult<PagedResult<TicketListItemResponse>>.Success(new PagedResult<TicketListItemResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        });
    }

    public async Task<AppResult<TicketDetailResponse>> UpdateAsync(
        Guid ticketId,
        UpdateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var ticket = await LoadTrackedTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketNotFound();
        }

        var editability = TicketMutability.EnsureCanEditFields(ticket.Status);
        if (!editability.IsAllowed)
        {
            return AppResult<TicketDetailResponse>.Failure(
                editability.ErrorCode!,
                editability.Reason!);
        }

        var title = request.Title.Trim();
        var description = request.Description.Trim();
        var customerName = request.CustomerName.Trim();
        var customerEmail = request.CustomerEmail.Trim();

        if (string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(description)
            || string.IsNullOrWhiteSpace(customerName)
            || string.IsNullOrWhiteSpace(customerEmail))
        {
            return AppResult<TicketDetailResponse>.Failure(
                AppErrorCodes.ValidationError,
                "Required ticket fields cannot be empty or whitespace.");
        }

        var priority = request.Priority
            ?? throw new InvalidOperationException("Priority must be validated before UpdateAsync.");

        var unchanged =
            ticket.Title == title
            && ticket.Description == description
            && ticket.CustomerName == customerName
            && ticket.CustomerEmail == customerEmail
            && ticket.Priority == priority;

        if (unchanged)
        {
            await EnsureAgentLoadedAsync(ticket, cancellationToken);
            return AppResult<TicketDetailResponse>.Success(
                TicketMapper.ToDetail(ticket, _clock.UtcNow));
        }

        var priorityChanged = ticket.Priority != priority;

        ticket.Title = title;
        ticket.Description = description;
        ticket.CustomerName = customerName;
        ticket.CustomerEmail = customerEmail;
        ticket.Priority = priority;

        if (priorityChanged)
        {
            ticket.DueDate = DueDateCalculator.Calculate(ticket.CreatedDate, priority);
        }

        ticket.LastModifiedDate = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await EnsureAgentLoadedAsync(ticket, cancellationToken);
        return AppResult<TicketDetailResponse>.Success(
            TicketMapper.ToDetail(ticket, _clock.UtcNow));
    }

    public async Task<AppResult<TicketDetailResponse>> AssignAsync(
        Guid ticketId,
        Guid agentId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await LoadTrackedTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketNotFound();
        }

        var closed = TicketMutability.EnsureNotClosed(ticket.Status);
        if (!closed.IsAllowed)
        {
            return AppResult<TicketDetailResponse>.Failure(closed.ErrorCode!, closed.Reason!);
        }

        var agent = await _db.Agents.FirstOrDefaultAsync(a => a.Id == agentId, cancellationToken);
        if (agent is null)
        {
            return AppResult<TicketDetailResponse>.Failure(
                AppErrorCodes.AgentNotFound,
                $"Agent '{agentId}' was not found.");
        }

        var assignDecision = TicketAssignmentPolicy.EvaluateAssign(agent.Active);
        if (!assignDecision.IsAllowed)
        {
            return AppResult<TicketDetailResponse>.Failure(
                assignDecision.ErrorCode!,
                assignDecision.Reason!);
        }

        ticket.AssignedAgentId = agent.Id;
        ticket.AssignedAgent = agent;
        ticket.LastModifiedDate = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return AppResult<TicketDetailResponse>.Success(
            TicketMapper.ToDetail(ticket, _clock.UtcNow));
    }

    public async Task<AppResult<TicketDetailResponse>> UnassignAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await LoadTrackedTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketNotFound();
        }

        var closed = TicketMutability.EnsureNotClosed(ticket.Status);
        if (!closed.IsAllowed)
        {
            return AppResult<TicketDetailResponse>.Failure(closed.ErrorCode!, closed.Reason!);
        }

        if (ticket.AssignedAgentId is null)
        {
            await EnsureAgentLoadedAsync(ticket, cancellationToken);
            return AppResult<TicketDetailResponse>.Success(
                TicketMapper.ToDetail(ticket, _clock.UtcNow));
        }

        ticket.AssignedAgentId = null;
        ticket.AssignedAgent = null;
        ticket.LastModifiedDate = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return AppResult<TicketDetailResponse>.Success(
            TicketMapper.ToDetail(ticket, _clock.UtcNow, assignedAgentActiveForTransitions: null));
    }

    public async Task<AppResult<TicketDetailResponse>> ChangeStatusAsync(
        Guid ticketId,
        Status requestedStatus,
        CancellationToken cancellationToken = default)
    {
        var ticket = await LoadTrackedTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return TicketNotFound();
        }

        var transition = TicketTransitionPolicy.Evaluate(ticket.Status, requestedStatus);
        if (!transition.IsAllowed)
        {
            return AppResult<TicketDetailResponse>.Failure(
                transition.ErrorCode!,
                transition.Reason!,
                new Dictionary<string, object?>
                {
                    ["currentStatus"] = ticket.Status.ToString(),
                    ["requestedStatus"] = requestedStatus.ToString(),
                    ["reason"] = transition.Reason
                });
        }

        if (requestedStatus == Status.Resolved)
        {
            if (ticket.AssignedAgentId is null)
            {
                return AppResult<TicketDetailResponse>.Failure(
                    AppErrorCodes.AssignmentRequired,
                    "An active assigned agent is required before resolving a ticket.",
                    new Dictionary<string, object?>
                    {
                        ["currentStatus"] = ticket.Status.ToString(),
                        ["requestedStatus"] = requestedStatus.ToString()
                    });
            }

            var agent = await _db.Agents
                .FirstOrDefaultAsync(a => a.Id == ticket.AssignedAgentId, cancellationToken);

            if (agent is null)
            {
                return AppResult<TicketDetailResponse>.Failure(
                    AppErrorCodes.AgentNotFound,
                    $"Assigned agent '{ticket.AssignedAgentId}' was not found.");
            }

            if (!agent.Active)
            {
                return AppResult<TicketDetailResponse>.Failure(
                    AppErrorCodes.AgentInactive,
                    "The assigned agent is inactive and cannot resolve the ticket.",
                    new Dictionary<string, object?>
                    {
                        ["currentStatus"] = ticket.Status.ToString(),
                        ["requestedStatus"] = requestedStatus.ToString(),
                        ["agentId"] = agent.Id
                    });
            }

            ticket.AssignedAgent = agent;
        }

        var now = _clock.UtcNow;

        switch (requestedStatus)
        {
            case Status.InProgress when ticket.Status == Status.New:
                ticket.Status = Status.InProgress;
                ticket.LastModifiedDate = now;
                break;

            case Status.Resolved when ticket.Status == Status.InProgress:
                ticket.Status = Status.Resolved;
                ticket.ResolvedDate = now;
                ticket.LastModifiedDate = now;
                break;

            case Status.Closed when ticket.Status == Status.Resolved:
                ticket.Status = Status.Closed;
                ticket.ClosedDate = now;
                ticket.LastModifiedDate = now;
                break;

            case Status.InProgress when ticket.Status == Status.Resolved:
                ticket.Status = Status.InProgress;
                ticket.ResolvedDate = null;
                // ClosedDate remains null
                ticket.LastModifiedDate = now;
                break;

            default:
                // Structural policy already validated; should be unreachable.
                return AppResult<TicketDetailResponse>.Failure(
                    AppErrorCodes.InvalidStatusTransition,
                    $"Transition from {ticket.Status} to {requestedStatus} is not allowed.",
                    new Dictionary<string, object?>
                    {
                        ["currentStatus"] = ticket.Status.ToString(),
                        ["requestedStatus"] = requestedStatus.ToString()
                    });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await EnsureAgentLoadedAsync(ticket, cancellationToken);

        return AppResult<TicketDetailResponse>.Success(
            TicketMapper.ToDetail(ticket, now));
    }

    public async Task<AppResult<CommentResponse>> AddCommentAsync(
        Guid ticketId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var ticket = await LoadTrackedTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return AppResult<CommentResponse>.Failure(
                AppErrorCodes.TicketNotFound,
                $"Ticket '{ticketId}' was not found.");
        }

        var closed = TicketMutability.EnsureNotClosed(ticket.Status);
        if (!closed.IsAllowed)
        {
            return AppResult<CommentResponse>.Failure(closed.ErrorCode!, closed.Reason!);
        }

        var authorName = request.AuthorName.Trim();
        var body = request.Body.Trim();
        if (string.IsNullOrWhiteSpace(authorName) || string.IsNullOrWhiteSpace(body))
        {
            return AppResult<CommentResponse>.Failure(
                AppErrorCodes.ValidationError,
                "Comment author and body cannot be empty or whitespace.");
        }

        var now = _clock.UtcNow;
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            TicketId = ticket.Id,
            AuthorName = authorName,
            Body = body,
            CreatedDate = now
        };

        _db.Comments.Add(comment);
        ticket.LastModifiedDate = now;
        await _db.SaveChangesAsync(cancellationToken);

        return AppResult<CommentResponse>.Success(CommentMapper.ToResponse(comment));
    }

    public async Task<AppResult> DeleteAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await LoadTrackedTicketAsync(ticketId, cancellationToken);
        if (ticket is null)
        {
            return AppResult.Failure(
                AppErrorCodes.TicketNotFound,
                $"Ticket '{ticketId}' was not found.");
        }

        var closed = TicketMutability.EnsureNotClosed(ticket.Status);
        if (!closed.IsAllowed)
        {
            return AppResult.Failure(closed.ErrorCode!, closed.Reason!);
        }

        _db.Tickets.Remove(ticket);
        await _db.SaveChangesAsync(cancellationToken);
        return AppResult.Success();
    }

    private async Task<Ticket?> LoadTrackedTicketAsync(Guid ticketId, CancellationToken cancellationToken) =>
        await _db.Tickets
            .Include(t => t.AssignedAgent)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

    private async Task EnsureAgentLoadedAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        if (ticket.AssignedAgentId is null || ticket.AssignedAgent is not null)
        {
            return;
        }

        ticket.AssignedAgent = await _db.Agents
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == ticket.AssignedAgentId, cancellationToken);
    }

    private static AppResult<TicketDetailResponse> TicketNotFound() =>
        AppResult<TicketDetailResponse>.Failure(
            AppErrorCodes.TicketNotFound,
            "Ticket was not found.");
}
