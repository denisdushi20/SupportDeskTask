using System.ComponentModel.DataAnnotations;

namespace SupportDesk.Api.Contracts;

public sealed class AssignAgentRequest
{
    /// <summary>Required. Nullable so omitted JSON does not silently become Guid.Empty.</summary>
    [Required]
    public Guid? AgentId { get; set; }
}
