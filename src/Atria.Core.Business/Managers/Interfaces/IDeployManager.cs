using Atria.Core.Business.Models.Dto.Feed;

namespace Atria.Core.Business.Managers.Interfaces;

public interface IDeployManager : IBaseManager
{
    Task<List<DeployDto>> GetDeploysByFeedIdAsync(Guid feedId, CancellationToken ct);
}
