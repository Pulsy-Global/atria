using Atria.Core.Data.UnitOfWork.Context;

namespace Atria.Core.Data.UnitOfWork.Factory;

public interface IUnitOfWorkFactory
{
    IUnitOfWork BuildContext(bool shareConnection = false);
}
