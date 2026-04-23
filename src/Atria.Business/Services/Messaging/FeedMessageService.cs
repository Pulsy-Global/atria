using Atria.Business.Services.Messaging.Interfaces;
using Atria.Business.Services.Messaging.Models;
using Atria.Common.Messaging.Core;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Subjects.Feed;
using Microsoft.Extensions.Options;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atria.Business.Services.Messaging;

public class FeedMessageService(
    NatsConnectionManager connectionManager,
    StreamManager streamManager,
    IOptions<MessagingSettings> settings)
    : IFeedMessageService
{
    private static readonly JsonSerializerOptions OutputJsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly NatsJSContext _js = connectionManager.JSContext;
    private readonly MessagingSettings _settings = settings.Value;

    public async Task<IEnumerable<FeedMessageModel>> GetFeedOutputsAsync(
        Guid feedId,
        int limit,
        CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(limit, 1);

        var subject = FeedSubjects.System.FeedOutput(_settings.JetStreamPrefix, feedId.ToString());

        await streamManager.EnsureStreamAsync(_settings.DefaultFeedStream, ct);

        var stream = await _js.GetStreamAsync(_settings.DefaultFeedStream, cancellationToken: ct);

        try
        {
            var response = await stream.GetAsync(
                new StreamMsgGetRequest { LastBySubj = subject },
                ct);

            var stored = response.Message;
            if (stored?.Data == null)
            {
                return [];
            }

            var outputData = JsonSerializer.Deserialize<FeedOutputData>(stored.Data.Span);

            if (outputData == null)
            {
                return [];
            }

            return
            [
                new FeedMessageModel
                {
                    SeqNumber = stored.Seq,
                    Data = JsonSerializer.Serialize(outputData.Data, OutputJsonOpts),
                    CreatedAt = stored.Time,
                    SizeBytes = stored.Data.Length,
                    IsTestExecution = outputData.IsTestExecution,
                    BlockNumber = outputData.BlockNumber,
                },
            ];
        }
        catch (NatsJSException)
        {
            return [];
        }
    }

    public async IAsyncEnumerable<FeedMessageModel> StreamFeedOutputsAsync(
        Guid feedId,
        ulong? afterSeq,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var subject = FeedSubjects.System.FeedOutput(_settings.JetStreamPrefix, feedId.ToString());

        await streamManager.EnsureStreamAsync(_settings.DefaultFeedStream, ct);

        var consumerConfig = afterSeq.HasValue
            ? new NatsJSOrderedConsumerOpts
            {
                FilterSubjects = [subject],
                DeliverPolicy = ConsumerConfigDeliverPolicy.ByStartSequence,
                OptStartSeq = afterSeq.Value + 1,
            }
            : new NatsJSOrderedConsumerOpts
            {
                FilterSubjects = [subject],
                DeliverPolicy = ConsumerConfigDeliverPolicy.New,
            };

        var consumer = await _js.CreateOrderedConsumerAsync(_settings.DefaultFeedStream, consumerConfig, ct);

        await foreach (var msg in consumer.ConsumeAsync<FeedOutputData>(cancellationToken: ct))
        {
            var meta = msg.Metadata;
            if (!meta.HasValue || msg.Data == null)
            {
                continue;
            }

            yield return new FeedMessageModel
            {
                SeqNumber = meta.Value.Sequence.Stream,
                Data = JsonSerializer.Serialize(msg.Data.Data, OutputJsonOpts),
                CreatedAt = meta.Value.Timestamp,
                SizeBytes = msg.Size,
                IsTestExecution = msg.Data.IsTestExecution,
                BlockNumber = msg.Data.BlockNumber,
            };
        }
    }
}
