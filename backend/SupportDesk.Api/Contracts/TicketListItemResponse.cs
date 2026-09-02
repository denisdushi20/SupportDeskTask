using SupportDesk.Domain.Enums;

namespace SupportDesk.Api.Contracts;

public sealed class TicketListItemResponse
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public Priority Priority { get; set; }
    public Status Status { get; set; }
    public Guid? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public DateTimeOffset LastModifiedDate { get; set; }
    public bool IsOverdue { get; set; }
}
