using Atria.Core.Data.Entities.Context;
using Atria.Core.Data.Entities.Context.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Tags;

public class Tag : BaseEntity<Guid>, IAuditCreated, IAuditUpdated, IAuditDeleted
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

    public ICollection<FeedTag> FeedTags { get; set; } = new List<FeedTag>();

    public ICollection<OutputTag> OutputTags { get; set; } = new List<OutputTag>();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
