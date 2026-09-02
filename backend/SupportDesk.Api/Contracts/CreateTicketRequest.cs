using System.ComponentModel.DataAnnotations;
using SupportDesk.Domain.Enums;

namespace SupportDesk.Api.Contracts;

public sealed class CreateTicketRequest
{
    [Required(AllowEmptyStrings = false)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    [MaxLength(320)]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>Required. Nullable so omitted JSON does not silently become Low.</summary>
    [Required]
    [EnumDataType(typeof(Priority))]
    public Priority? Priority { get; set; }
}
