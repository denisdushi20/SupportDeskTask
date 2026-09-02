using System.ComponentModel.DataAnnotations;
using SupportDesk.Domain.Enums;

namespace SupportDesk.Api.Contracts;

public sealed class ChangeStatusRequest
{
    /// <summary>Required. Nullable so omitted JSON does not silently become New.</summary>
    [Required]
    [EnumDataType(typeof(Status))]
    public Status? Status { get; set; }
}
