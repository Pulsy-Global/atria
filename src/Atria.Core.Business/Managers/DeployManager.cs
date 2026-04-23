using Atria.Business.Services.DataServices.Interfaces;
using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Feed;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Atria.Core.Business.Managers;

public class DeployManager : BaseManager, IDeployManager
{
    private readonly IDeployDataService _deployDataService;

    public DeployManager(
        IDeployDataService deployDataService,
        ILogger<DeployManager> logger,
        IMapper mapper)
        : base(logger, mapper)
    {
        _deployDataService = deployDataService;
    }

    public async Task<List<DeployDto>> GetDeploysByFeedIdAsync(Guid feedId, CancellationToken ct)
    {
        var entities = await _deployDataService.GetDeploysAsync(x => x.FeedId == feedId, ct);

        var sortedEntities = entities
            .OrderByDescending(x => x.CreatedAt)
            .ToList();

        return Mapper.Map<List<DeployDto>>(sortedEntities);
    }
}
