using Atria.Business.Services.DataServices.Interfaces;
using Atria.Common.Models.Generic;
using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Output;
using Atria.Core.Business.Models.Dto.Output.Config;
using Atria.Core.Business.Services.Probe;
using Atria.Core.Data.Entities.Outputs;
using Atria.Core.Data.Entities.Outputs.Config;
using Atria.Core.Data.Extensions;
using Atria.Core.Data.Models.Query;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Atria.Core.Business.Managers;

public class OutputManager : BaseManager, IOutputManager
{
    private readonly IOutputDataService _outputDataService;
    private readonly IWebhookProbeService _webhookProbeService;

    public OutputManager(
        IOutputDataService outputDataService,
        IWebhookProbeService webhookProbeService,
        ILogger<OutputManager> logger,
        IMapper mapper)
        : base(logger, mapper)
    {
        _outputDataService = outputDataService;
        _webhookProbeService = webhookProbeService;
    }

    public async Task<OutputDto> CreateOutputAsync(CreateOutputDto dto, CancellationToken ct)
    {
        if (dto.Config is WebhookDto webhookDto)
        {
            await _webhookProbeService.ProbeAsync(webhookDto, ct);
        }

        var entity = Mapper.Map<Output>(dto);

        await _outputDataService.CreateOutputAsync(entity, ct, dto.TagIds);

        return Mapper.Map<OutputDto>(entity);
    }

    public async Task<OutputDto> UpdateOutputAsync(Guid id, UpdateOutputDto dto, CancellationToken ct)
    {
        var entity = await _outputDataService.GetOutputByIdAsync(id, ct, x => x.OutputTags);

        if (dto.Config is WebhookDto webhookDto
            && (entity.Config is not WebhookOutputConfig existingWebhook
                || !string.Equals(existingWebhook.Url, webhookDto.Url, StringComparison.Ordinal)))
        {
            await _webhookProbeService.ProbeAsync(webhookDto, ct);
        }

        Mapper.Map(dto, entity);

        await _outputDataService.UpdateOutputAsync(entity, ct, dto.TagIds);

        return Mapper.Map<OutputDto>(entity);
    }

    public async Task<OutputDto> GetOutputAsync(Guid id, CancellationToken ct)
    {
        var entity = await _outputDataService.GetOutputByIdAsync(id, ct, x => x.OutputTags);

        return Mapper.Map<OutputDto>(entity);
    }

    public async Task<PagedList<OutputDto>> GetOutputsAsync(QueryOptions<OutputDto> queryOptions, CancellationToken ct)
    {
        var mappedQuery = Mapper.MapQueryOptions<OutputDto, Output>(queryOptions);

        var entities = await _outputDataService.GetOutputsAsync(mappedQuery, ct);

        return Mapper.Map<PagedList<OutputDto>>(entities);
    }

    public async Task<List<OutputDto>> GetOutputsByFeedIdAsync(Guid feedId, CancellationToken ct)
    {
        var outputs = await _outputDataService.GetOutputsAsync(
            x => x.FeedOutputs.Any(x => x.FeedId == feedId), ct, x => x.OutputTags);

        return Mapper.Map<List<OutputDto>>(outputs);
    }

    public async Task DeleteOutputAsync(Guid id, CancellationToken ct)
    {
        await _outputDataService.DeleteOutputAsync(id, ct);
    }
}
