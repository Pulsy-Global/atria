using Atria.Core.Business.Managers.Interfaces;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Atria.Core.Business.Managers;

public abstract class BaseManager : IBaseManager
{
    public IMapper Mapper { get; }

    public ILogger Logger { get; }

    protected BaseManager(
        ILogger logger,
        IMapper mapper)
    {
        Logger = logger;
        Mapper = mapper;
    }
}
