using SupportDesk.Domain.Enums;

namespace SupportDesk.Api.Contracts;

public sealed class AgentListItemResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Department Department { get; set; }
    public bool Active { get; set; }
}
