using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Atria.Core.Business.Managers.Interfaces;

public interface IBaseManager
{
    IMapper Mapper { get; }

    ILogger Logger { get; }
}
