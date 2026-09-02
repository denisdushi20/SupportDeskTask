using SupportDesk.Api.Contracts;
using SupportDesk.Domain.Entities;
using SupportDesk.Domain.Enums;
using SupportDesk.Domain.Policies;

namespace SupportDesk.Api.Application.Mapping;

public static class TicketMapper
{
    public static TicketDetailResponse ToDetail(
        Ticket ticket,
        DateTimeOffset utcNow,
        bool? assignedAgentActiveForTransitions = null)
    {
        var agentActive = assignedAgentActiveForTransitions
            ?? ticket.AssignedAgent?.Active;

        return new TicketDetailResponse
        {
            Id = ticket.Id,
            Reference = ticket.Reference,
            Title = ticket.Title,
            Description = ticket.Description,
            CustomerName = ticket.CustomerName,
            CustomerEmail = ticket.CustomerEmail,
            Priority = ticket.Priority,
            Status = ticket.Status,
            AssignedAgentId = ticket.AssignedAgentId,
            AssignedAgent = ticket.AssignedAgent is null
                ? null
                : AgentMapper.ToSummary(ticket.AssignedAgent),
            CreatedDate = ticket.CreatedDate,
            LastModifiedDate = ticket.LastModifiedDate,
            ResolvedDate = ticket.ResolvedDate,
            ClosedDate = ticket.ClosedDate,
            DueDate = ticket.DueDate,
            IsOverdue = OverdueEvaluator.IsOverdue(ticket.DueDate, ticket.Status, utcNow),
            AllowedTransitions = ComputeAllowedTransitions(ticket, agentActive),
            CanEditFields = TicketMutability.CanEditFields(ticket.Status),
            CanAssign = TicketMutability.CanMutate(ticket.Status),
            CanUnassign = TicketMutability.CanMutate(ticket.Status),
            CanAddComment = TicketMutability.CanMutate(ticket.Status),
            CanDelete = TicketMutability.CanMutate(ticket.Status),
            Comments = ticket.Comments
                .OrderBy(c => c.CreatedDate)
                .ThenBy(c => c.Id)
                .Select(CommentMapper.ToResponse)
                .ToList()
        };
    }

    public static IReadOnlyList<Status> ComputeAllowedTransitions(
        Ticket ticket,
        bool? assignedAgentIsActive)
    {
        var allowed = new List<Status>();

        foreach (Status target in Enum.GetValues<Status>())
        {
            if (!TicketTransitionPolicy.IsAllowed(ticket.Status, target))
            {
                continue;
            }

            // UX helper: hide Resolve when assignment/active-agent contextual rule would fail.
            if (target == Status.Resolved)
            {
                if (ticket.AssignedAgentId is null || assignedAgentIsActive != true)
                {
                    continue;
                }
            }

            allowed.Add(target);
        }

        return allowed;
    }
}

public static class CommentMapper
{
    public static CommentResponse ToResponse(Comment comment) => new()
    {
        Id = comment.Id,
        AuthorName = comment.AuthorName,
        Body = comment.Body,
        CreatedDate = comment.CreatedDate
    };
}

public static class AgentMapper
{
    public static AgentSummaryResponse ToSummary(Agent agent) => new()
    {
        Id = agent.Id,
        FullName = agent.FullName,
        Email = agent.Email,
        Department = agent.Department,
        Active = agent.Active
    };

    public static AgentListItemResponse ToListItem(Agent agent) => new()
    {
        Id = agent.Id,
        FullName = agent.FullName,
        Email = agent.Email,
        Department = agent.Department,
        Active = agent.Active
    };
}
