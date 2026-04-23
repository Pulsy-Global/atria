using Atria.Core.Data.Entities.Context;
using Atria.Core.Data.Entities.Context.Interfaces;
using Atria.Core.Data.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Feeds;

public class FeedStatusChange : BaseEntity<Guid>, IAuditCreated, IAuditDeleted
{
    [Required]
    public Guid FeedId { get; set; }

    [Required]
    public FeedStatus FromStatus { get; set; }

    [Required]
    public FeedStatus ToStatus { get; set; }

    public Feed Feed { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
