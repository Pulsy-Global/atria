using Atria.Core.Data.Entities.Context;
using Atria.Core.Data.Entities.Context.Interfaces;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Entities.Outputs.Config;
using Atria.Core.Data.Entities.Tags;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Outputs;

public class Output : BaseEntity<Guid>, IAuditCreated, IAuditUpdated, IAuditDeleted
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }
    [MaxLength(500)]
    public string? Description { get; set; }

    public OutputType Type { get; protected set; }

    [Required]
    [MaxLength(64)]
    public string Hash { get; set; }

    public OutputConfigBase Config { get; set; }

    public ICollection<FeedOutput> FeedOutputs { get; set; } = new List<FeedOutput>();

    public ICollection<OutputTag> OutputTags { get; set; } = new List<OutputTag>();

    public string? SearchContent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
