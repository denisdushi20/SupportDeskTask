namespace SupportDesk.Domain.Entities;

public class Comment
{
    public Guid Id { get; set; }

    public Guid TicketId { get; set; }

    public Ticket? Ticket { get; set; }

    public string AuthorName { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedDate { get; set; }
}
