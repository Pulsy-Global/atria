namespace Atria.Core.Business.Models.Metrics;

public sealed class FeedMetricsDto
{
    public Guid FeedId { get; init; }

    public string Range { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public DateTimeOffset GeneratedAt { get; init; }

    public DateTimeOffset Start { get; init; }

    public DateTimeOffset End { get; init; }

    public int ResolutionSeconds { get; init; }

    public FeedMetricsSummaryDto Summary { get; init; } = new();

    public IReadOnlyList<FeedMetricSeriesDto> Series { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];
}
