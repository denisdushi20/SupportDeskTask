using System.ComponentModel.DataAnnotations;
using SupportDesk.Domain.Enums;

namespace SupportDesk.Api.Contracts;

public sealed class TicketListQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [MaxLength(200)]
    public string? Search { get; set; }

    public Status? Status { get; set; }

    public Priority? Priority { get; set; }

    public Guid? AssignedAgentId { get; set; }

    public bool OverdueOnly { get; set; }
}
