namespace Atria.Feed.Delivery.FeedPipeline.Interfaces;

public interface IFeedPipeline
{
    Task ExecutePipelineAsync(
        string feedId,
        List<string> outputIds,
        object? data,
        bool isTestExecution,
        CancellationToken ct = default);
}
