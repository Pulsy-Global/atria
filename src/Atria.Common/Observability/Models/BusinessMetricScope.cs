namespace Atria.Common.Observability.Models;

public sealed record BusinessMetricScope(string ResourceNamespace, string FeedId)
{
    public static BusinessMetricScope? Create(string? resourceNamespace, string feedId)
    {
        return string.IsNullOrWhiteSpace(resourceNamespace) || string.IsNullOrWhiteSpace(feedId)
            ? null
            : new BusinessMetricScope(resourceNamespace, feedId);
    }
}
