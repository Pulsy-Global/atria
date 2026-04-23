using Atria.Core.Data.Entities.Enums;
using Atria.Core.Data.Entities.Outputs;
using Atria.Core.Data.Entities.Outputs.Config;
using Atria.Core.Data.UnitOfWork.Factory;
using Atria.Orchestrator.Config.Options;
using Atria.Orchestrator.Models.Dto.Connections;
using Atria.Orchestrator.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atria.Orchestrator.Managers;

public class OutputProvisioningManager : IOutputProvisioningManager
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly Dictionary<OutputType, Func<JsonElement, OutputConfigBase?>> _configResolvers = new()
    {
        [OutputType.Webhook] = element => element.Deserialize<WebhookOutputConfig>(_jsonOptions),
    };

    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IManifestScanner _manifestScanner;
    private readonly OrchestratorOptions _orchestratorOptions;
    private readonly ILogger<OutputProvisioningManager> _logger;

    public OutputProvisioningManager(
        ILogger<OutputProvisioningManager> logger,
        IUnitOfWorkFactory uowFactory,
        IManifestScanner directoryScanner,
        IOptions<OrchestratorOptions> orchestratorOptions)
    {
        _logger = logger;
        _uowFactory = uowFactory;
        _manifestScanner = directoryScanner;
        _orchestratorOptions = orchestratorOptions.Value;
    }

    public async Task ExecuteProvisioningAsync(CancellationToken ct = default)
    {
        var configs = await _manifestScanner.ScanDirectoryAsync<OutputsConfig>(
            _orchestratorOptions.Provisioning.OutputsDirectory,
            "outputs.json");

        using var uow = _uowFactory.BuildContext();

        foreach (var config in configs)
        {
            foreach (var output in config.Item.Outputs)
            {
                if (!_configResolvers.TryGetValue(output.Type, out var resolver))
                {
                    _logger.LogWarning("No resolver found for output type {OutputType}", output.Type);
                    continue;
                }

                var element = (JsonElement)output.Config;
                var outputConfig = resolver(element);

                if (outputConfig == null)
                {
                    continue;
                }

                var existing = await uow.OutputRepository.GetAsync(x => x.Name == output.Id, ct);

                var hash = config.FileHash;
                if (existing == null)
                {
                    var newCp = new Output
                    {
                        Name = output.Id,
                        Hash = hash,
                        Config = outputConfig,
                    };

                    await uow.OutputRepository.CreateAsync(newCp, ct);

                    await uow.SaveChangesAsync(ct);
                }
                else if (existing.Hash != hash)
                {
                    existing.Hash = hash;
                    existing.Config = outputConfig;

                    uow.OutputRepository.Update(existing);

                    await uow.SaveChangesAsync(ct);
                }
            }
        }
    }
}
