namespace Atria.Core.Business.Models.Metrics;

public sealed record MetricsStoreQuery(
    string Expression,
    DateTimeOffset Start,
    DateTimeOffset End,
    TimeSpan Step);
