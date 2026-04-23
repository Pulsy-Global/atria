using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Atria.Orchestrator.Managers.Interfaces;

public interface IBaseManager
{
    IMapper Mapper { get; }

    ILogger Logger { get; }
}
