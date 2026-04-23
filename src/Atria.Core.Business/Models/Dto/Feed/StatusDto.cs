namespace Atria.Core.Business.Models.Dto.Feed;

public class StatusDto
{
    public Guid FeedId { get; set; }
    public ulong? FeedCursor { get; set; }
    public ulong ChainHead { get; set; }
}
