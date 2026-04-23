using Atria.Core.Data.Entities.Context;
using Atria.Core.Data.Entities.Context.Interfaces;
using Atria.Core.Data.Entities.Deploys;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Tags;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Feeds;

public class Feed : BaseEntity<Guid>, IAuditCreated, IAuditUpdated, IAuditDeleted
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [Required]
    [MaxLength(25)]
    public string Version { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public FeedStatus Status { get; set; } = FeedStatus.Draft;

    [Required]
    [MaxLength(50)]
    public string NetworkId { get; set; }

    public AtriaDataType DataType { get; set; }

    public ulong? StartBlock { get; set; }

    public ulong? EndBlock { get; set; }

    public int BlockDelay { get; set; }

    public bool IsLocal { get; set; } = false;

    [Required]
    public ErrorHandlingStrategy ErrorHandling { get; set; }

    [MaxLength(255)]
    public string? FilterPath { get; set; }

    [MaxLength(255)]
    public string? FunctionPath { get; set; }

    [Required]
    [MaxLength(64)]
    public string Hash { get; set; }

    public ICollection<FeedOutput> FeedOutputs { get; set; } = new List<FeedOutput>();

    public ICollection<FeedStatusChange> StatusChanges { get; set; } = new List<FeedStatusChange>();

    public ICollection<Deploy> Deploys { get; set; } = new List<Deploy>();

    public ICollection<FeedTag> FeedTags { get; set; } = new List<FeedTag>();

    public string? SearchContent { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
