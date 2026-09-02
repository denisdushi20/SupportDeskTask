using SupportDesk.Domain.Enums;

namespace SupportDesk.Api.Contracts;

public sealed class TicketDetailResponse
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public Status Status { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public AgentSummaryResponse? AssignedAgent { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset LastModifiedDate { get; set; }
    public DateTimeOffset? ResolvedDate { get; set; }
    public DateTimeOffset? ClosedDate { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public bool IsOverdue { get; set; }
    public IReadOnlyList<Status> AllowedTransitions { get; set; } = Array.Empty<Status>();
    public bool CanEditFields { get; set; }
    public bool CanAssign { get; set; }
    public bool CanUnassign { get; set; }
    public bool CanAddComment { get; set; }
    public bool CanDelete { get; set; }
    public IReadOnlyList<CommentResponse> Comments { get; set; } = Array.Empty<CommentResponse>();
}
