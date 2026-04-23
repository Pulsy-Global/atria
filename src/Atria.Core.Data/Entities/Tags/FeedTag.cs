using Atria.Core.Data.Entities.Context;
using Atria.Core.Data.Entities.Feeds;
using System.ComponentModel.DataAnnotations;

namespace Atria.Core.Data.Entities.Tags;

public class FeedTag : BaseEntity<Guid>
{
    [Required]
    public Guid FeedId { get; set; }

    [Required]
    public Guid TagId { get; set; }

    public Feed Feed { get; set; } = null!;

    public Tag Tag { get; set; } = null!;
}
