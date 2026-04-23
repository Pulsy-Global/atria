using Atria.Core.Data.Entities.Enums;

namespace Atria.Core.Business.Models.Dto.Feed;

public class DeployDto
{
    public Guid Id { get; set; }

    public Guid FeedId { get; set; }

    public string Version { get; set; }

    public DeployStatus Status { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
