using SupportDesk.Domain.Enums;

namespace SupportDesk.Domain.Entities;

public class Ticket
{
    public Guid Id { get; set; }

    /// <summary>Server-generated human-readable reference, e.g. TCK-2026-0001.</summary>
    public string Reference { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public Priority Priority { get; set; }

    public Status Status { get; set; }

    public Guid? AssignedAgentId { get; set; }

    public Agent? AssignedAgent { get; set; }

    public DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    /// Timestamp of the latest successful mutation/activity affecting this ticket,
    /// including field updates, assignment changes, status transitions, and comment creation.
    /// </summary>
    public DateTimeOffset LastModifiedDate { get; set; }

    public DateTimeOffset? ResolvedDate { get; set; }

    public DateTimeOffset? ClosedDate { get; set; }

    /// <summary>Server-calculated from CreatedDate + Priority. Never client-provided.</summary>
    public DateTimeOffset DueDate { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
