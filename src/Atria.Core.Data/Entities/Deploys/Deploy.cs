using Atria.Core.Data.Entities.Context;
using Atria.Core.Data.Entities.Context.Interfaces;
using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Feeds;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Deploys;

public class Deploy : BaseEntity<Guid>, IAuditCreated, IAuditDeleted
{
    [Required]
    public Guid FeedId { get; set; }

    [Required]
    [MaxLength(25)]
    public string Version { get; set; }

    [Required]
    public DeployStatus Status { get; set; } = DeployStatus.None;

    public Feed Feed { get; set; }

    public ICollection<DeployStatusChange> StatusChanges { get; set; } = new List<DeployStatusChange>();

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
