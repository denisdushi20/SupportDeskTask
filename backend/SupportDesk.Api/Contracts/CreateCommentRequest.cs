using System.ComponentModel.DataAnnotations;

namespace SupportDesk.Api.Contracts;

public sealed class CreateCommentRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(200)]
    public string AuthorName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;
}
