using Atria.Common.Observability;
using Atria.Feed.Ingestor.Config.Options;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;
using System.Numerics;

namespace Atria.Feed.Ingestor.Observability;

public sealed class IngestorMetricsRecorder
{
    public const string BlockDataOperation = "block_data";
    public const string ChainHeadOperation = "chain_head";
    public const string RateLimitReason = "rate_limit";
    public const string TransientReason = "transient";
    public const string Success = "success";
    public const string Failure = "failure";

    private static readonly Histogram<double> FetchDuration = AtriaMeters.Observability.CreateHistogram<double>(
        "atria.ingestor.block.fetch.duration",
        "s");
    private static readonly Histogram<double> StoreDuration = AtriaMeters.Observability.CreateHistogram<double>(
        "atria.ingestor.block.store.duration",
        "s");
    private static readonly Counter<long> WebSocketLifecycle = AtriaMeters.Observability.CreateCounter<long>(
        "atria.ingestor.websocket.lifecycle",
        "{event}");
    private static readonly Counter<long> Reorganizations = AtriaMeters.Observability.CreateCounter<long>(
        "atria.ingestor.reorgs",
        "{event}");
    private static readonly Histogram<long> ReorganizationDepth = AtriaMeters.Observability.CreateHistogram<long>(
        "atria.ingestor.reorg.depth",
        "{block}");
    private static readonly Counter<long> RpcRetries = AtriaMeters.Observability.CreateCounter<long>(
        "atria.ingestor.rpc.retries",
        "{attempt}");
    private static readonly Counter<long> CircuitBreakerEvents = AtriaMeters.Observability.CreateCounter<long>(
        "atria.ingestor.circuit_breaker.events",
        "{event}");

    private readonly string _chain;
    private long _chainHead;
    private long _lastProcessed;
    private int _fetchesInFlight;
    private int _storesInFlight;

    public IngestorMetricsRecorder(IOptions<IngestorNetworkOptions> options)
    {
        _chain = options.Value.NetworkOptions.Id;
        AtriaMeters.Observability.CreateObservableGauge(
            "atria.ingestor.chain.head",
            () => Observe(Volatile.Read(ref _chainHead)),
            "{block}");
        AtriaMeters.Observability.CreateObservableGauge(
            "atria.ingestor.block.last_processed",
            () => Observe(Volatile.Read(ref _lastProcessed)),
            "{block}");
        AtriaMeters.Observability.CreateObservableGauge(
            "atria.ingestor.chain.lag",
            () => Observe(Math.Max(0, Volatile.Read(ref _chainHead) - Volatile.Read(ref _lastProcessed))),
            "{block}");
        AtriaMeters.Observability.CreateObservableGauge(
            "atria.ingestor.blocks.in_flight",
            ObserveInFlight,
            "{block}");
    }

    public void SetChainHead(BigInteger value) => Interlocked.Exchange(ref _chainHead, ToInt64(value));

    public void SetLastProcessed(BigInteger value) => Interlocked.Exchange(ref _lastProcessed, ToInt64(value));

    public void FetchStarted() => Interlocked.Increment(ref _fetchesInFlight);

    public void FetchCompleted(string operation, bool succeeded, TimeSpan duration)
    {
        Interlocked.Decrement(ref _fetchesInFlight);
        FetchDuration.Record(duration.TotalSeconds, CreateOperationTags(operation, succeeded));
    }

    public void StoreStarted() => Interlocked.Increment(ref _storesInFlight);

    public void StoreCompleted(bool succeeded, bool isReorg, TimeSpan duration)
    {
        Interlocked.Decrement(ref _storesInFlight);
        StoreDuration.Record(
            duration.TotalSeconds,
            new KeyValuePair<string, object?>("chain", _chain),
            new KeyValuePair<string, object?>("outcome", succeeded ? Success : Failure),
            new KeyValuePair<string, object?>("reorg", isReorg));
    }

    public void RecordWebSocketEvent(string eventName) => WebSocketLifecycle.Add(
        1,
        new KeyValuePair<string, object?>("chain", _chain),
        new KeyValuePair<string, object?>("event", eventName));

    public void RecordReorganization(bool succeeded, BigInteger depth)
    {
        Reorganizations.Add(
            1,
            new KeyValuePair<string, object?>("chain", _chain),
            new KeyValuePair<string, object?>("outcome", succeeded ? Success : Failure));
        if (succeeded)
        {
            ReorganizationDepth.Record(ToInt64(depth), new KeyValuePair<string, object?>("chain", _chain));
        }
    }

    public void RecordRpcRetry(string operation, string reason) => RpcRetries.Add(
        1,
        new KeyValuePair<string, object?>("chain", _chain),
        new KeyValuePair<string, object?>("operation", operation),
        new KeyValuePair<string, object?>("reason", reason));

    public void RecordCircuitBreakerEvent(string eventName) => CircuitBreakerEvents.Add(
        1,
        new KeyValuePair<string, object?>("chain", _chain),
        new KeyValuePair<string, object?>("event", eventName));

    private static long ToInt64(BigInteger value)
    {
        return value > long.MaxValue ? long.MaxValue : value < long.MinValue ? long.MinValue : (long)value;
    }

    private Measurement<long> Observe(long value)
    {
        return new Measurement<long>(value, new KeyValuePair<string, object?>("chain", _chain));
    }

    private IEnumerable<Measurement<int>> ObserveInFlight()
    {
        return
        [
            new Measurement<int>(
                Volatile.Read(ref _fetchesInFlight),
                new KeyValuePair<string, object?>("chain", _chain),
                new KeyValuePair<string, object?>("stage", "fetch")),
            new Measurement<int>(
                Volatile.Read(ref _storesInFlight),
                new KeyValuePair<string, object?>("chain", _chain),
                new KeyValuePair<string, object?>("stage", "store")),
        ];
    }

    private KeyValuePair<string, object?>[] CreateOperationTags(string operation, bool succeeded)
    {
        return
        [
            new KeyValuePair<string, object?>("chain", _chain),
            new KeyValuePair<string, object?>("operation", operation),
            new KeyValuePair<string, object?>("outcome", succeeded ? Success : Failure),
        ];
    }
}
