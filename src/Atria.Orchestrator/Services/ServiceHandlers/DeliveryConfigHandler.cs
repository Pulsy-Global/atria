using Atria.Business.Services.DataServices.Interfaces;
using Atria.Common.Messaging.ServiceBus;
using Atria.Contracts.Events.Feed;
using Atria.Contracts.Subjects.Feed;
using Atria.Core.Data.Entities.Outputs.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.Json;

namespace Atria.Orchestrator.Services.ServiceHandlers;

public sealed class DeliveryConfigHandler : ServiceBusHandler<FeedDeliveryTargetRequest>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceBus _serviceBus;

    public DeliveryConfigHandler(
        IServiceBus serviceBus,
        IServiceProvider serviceProvider,
        ILogger<DeliveryConfigHandler> logger)
        : base(serviceBus, logger)
    {
        _serviceProvider = serviceProvider;
        _serviceBus = serviceBus;
    }

    protected override string Subject => FeedSubjects.System.DeliveryConfigRequest;

    protected override string? QueueGroup => nameof(DeliveryConfigHandler);

    protected override Task HandleAsync(FeedDeliveryTargetRequest req, CancellationToken ct)
        => Task.CompletedTask;

    protected override async Task HandleAsync(FeedDeliveryTargetRequest req, string? replyTo, CancellationToken ct)
    {
        Logger.LogInformation(JsonSerializer.Serialize(req));

        if (string.IsNullOrEmpty(replyTo))
        {
            Logger.LogWarning("Received delivery config request without ReplyTo, skipping");
            return;
        }

        try
        {
            var id = Guid.Parse(req.Id);
            var pipeline = await GetFeedDeliveryPipelineAsync(id, ct);

            await _serviceBus.PublishAsync(replyTo, pipeline, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to get delivery config for request {Id}", req.Id);
            await _serviceBus.PublishAsync<FeedDeliveryTarget?>(replyTo, null, ct);
        }
    }

    private async Task<FeedDeliveryTarget?> GetFeedDeliveryPipelineAsync(Guid id, CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var outputDataService = scope.ServiceProvider.GetRequiredService<IOutputDataService>();

        var output = await outputDataService.GetOutputByIdAsync(id: id, ct);

        if (output is not { Config: WebhookOutputConfig })
        {
            throw new DataException("Unsupported config type");
        }

        var webhookConfig = outputDataService.GetTypedConfig<WebhookOutputConfig>(output);

        if (webhookConfig != null && !string.IsNullOrEmpty(webhookConfig.Url))
        {
            return new FeedWebhookTarget(
                Id: id.ToString(),
                Url: webhookConfig.Url,
                Method: webhookConfig.Method.ToString().ToUpperInvariant(),
                Headers: webhookConfig.Headers,
                TimeoutSeconds: webhookConfig.TimeoutSeconds);
        }

        throw new ArgumentException("No delivery pipeline found for the given output ID");
    }
}
