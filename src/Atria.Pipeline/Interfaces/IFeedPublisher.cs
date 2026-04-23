namespace Atria.Pipeline.Interfaces;

public interface IFeedPublisher
{
    Task PublishResultAsync<T>(string feedId, T data, CancellationToken ct = default);
}
