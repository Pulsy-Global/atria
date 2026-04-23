using Atria.Business.Services.Messaging.Models;
namespace Atria.Business.Services.Messaging.Interfaces;

public interface IFeedMessageService
{
    Task<IEnumerable<FeedMessageModel>> GetFeedOutputsAsync(
        Guid feedId,
        int limit,
        CancellationToken ct);

    IAsyncEnumerable<FeedMessageModel> StreamFeedOutputsAsync(
        Guid feedId,
        ulong? afterSeq,
        CancellationToken ct);
}
