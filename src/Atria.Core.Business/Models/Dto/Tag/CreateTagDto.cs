using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Business.Models.Dto.Tag;

public class CreateTagDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public string Type { get; set; }

    [Required]
    [MaxLength(7)]
    public string Color { get; set; } = "#000000";
}
