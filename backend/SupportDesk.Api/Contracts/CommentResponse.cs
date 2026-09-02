namespace SupportDesk.Api.Contracts;

public sealed class CommentResponse
{
    public Guid Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }
}
