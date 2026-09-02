using System.ComponentModel.DataAnnotations;

namespace SupportDesk.Api.Contracts;

public sealed class AgentListQuery
{
    [MaxLength(200)]
    public string? Search { get; set; }
}