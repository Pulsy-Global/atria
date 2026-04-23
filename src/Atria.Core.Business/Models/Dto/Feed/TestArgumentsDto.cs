using Atria.Core.Data.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Business.Models.Dto.Feed;

public class TestRequestDto
{
    [Required]
    [MaxLength(255)]
    public string BlockchainId { get; set; }

    [Required]
    public AtriaDataType DataType { get; set; }

    [Required]
    [MaxLength(255)]
    public string BlockNumber { get; set; }

    public string? FilterCode { get; set; }

    public string? FunctionCode { get; set; }

    public bool ExecuteOutputs { get; set; } = false;

    public List<string>? OutputsIds { get; set; } = null;
}
