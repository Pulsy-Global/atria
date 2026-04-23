using Atria.Core.Data.Entities.Context;
using Atria.Core.Data.Entities.Context.Interfaces;
using Atria.Core.Data.Entities.Outputs;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Feeds;

public class FeedOutput : BaseEntity<Guid>, IAuditCreated, IAuditDeleted
{
    [Required]
    public Guid FeedId { get; set; }

    [Required]
    public Guid OutputId { get; set; }

    public Feed Feed { get; set; }

    public Output Output { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
