using SupportDesk.Domain.Enums;

namespace SupportDesk.Domain.Entities;

public class Agent
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public Department Department { get; set; }

    public bool Active { get; set; } = true;

    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
