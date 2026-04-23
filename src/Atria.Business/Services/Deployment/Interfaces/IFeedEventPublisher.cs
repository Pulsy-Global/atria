using Atria.Business.Models;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Events.Feed.Enums;

namespace Atria.Business.Services.Deployment.Interfaces;

public interface IFeedEventPublisher
{
    Task<TestResult> ExecuteFeedTestAsync(TestRequest request, FeedDataType dataType, CancellationToken ct = default);

    Task PublishFeedDeployAsync(FeedDeployRequest request, CancellationToken ct = default);

    Task PublishFeedPauseAsync(Guid feedId, CancellationToken ct = default);

    Task PublishFeedDeleteAsync(Guid feedId, CancellationToken ct = default);
}
