using Atria.Core.Data.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Business.Models.Dto.Feed;

public class UpdateFeedDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    [MaxLength(25)]
    public string Version { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string NetworkId { get; set; }

    public AtriaDataType DataType { get; set; }

    public ulong? StartBlock { get; set; }

    public ulong? EndBlock { get; set; }

    [Required]
    public ErrorHandlingStrategy ErrorHandling { get; set; }

    public string? FilterCode { get; set; }

    public string? FunctionCode { get; set; }

    public List<Guid> OutputIds { get; set; } = new List<Guid>();

    public List<Guid> TagIds { get; set; } = new List<Guid>();

    [Range(0, 100)]
    public int BlockDelay { get; set; }
}
