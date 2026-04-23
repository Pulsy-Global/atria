namespace Atria.Feed.Ingestor.Config.Options;

public class IngestorOptions
{
    public bool EnableRealtimeIngestion { get; set; }

    public string NetworkId { get; set; }

    public int MaxReorgDepth { get; set; } = 1000;

    public int RetryAttempts { get; set; } = 3;

    public int RetryDelaySeconds { get; set; } = 1;

    public int BlockPollIntervalSec { get; set; } = 30;

    public int CatchUpBatchSize { get; set; } = 3;

    public int WsInactivityTimeoutSec { get; set; } = 60;

    public int WsReconnectDelaySec { get; set; } = 3;

    public IngestorHttpClientOptions HttpClient { get; set; } = new();
}
