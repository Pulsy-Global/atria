using Atria.Core.Data.Context;
using Atria.Core.Data.Repositories.Context.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Atria.Core.Data.UnitOfWork.Factory;

public class UnitOfWorkFactory : IUnitOfWorkFactory
{
    private readonly IServiceProvider _serviceProvider;

    public UnitOfWorkFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public (IServiceProvider, IServiceScope?) GetProvider(bool shareConnection = false)
    {
        IServiceProvider serviceProvider;
        IServiceScope? scope = null;

        if (shareConnection)
        {
            serviceProvider = _serviceProvider;
        }
        else
        {
            var scopeFactory = _serviceProvider
                .GetRequiredService<IServiceScopeFactory>();

            scope = scopeFactory.CreateScope();

            serviceProvider = scope.ServiceProvider;
        }

        return (serviceProvider, scope);
    }

    public Context.IUnitOfWork BuildContext(bool shareConnection = false)
    {
        var (sp, scope) = GetProvider(shareConnection);

        return new Context.UnitOfWork(
            sp.GetRequiredService<AtriaDbContext>(),
            sp.GetRequiredService<IFeedRepository>(),
            sp.GetRequiredService<IFeedOutputRepository>(),
            sp.GetRequiredService<IFeedTagRepository>(),
            sp.GetRequiredService<IOutputRepository>(),
            sp.GetRequiredService<IOutputTagRepository>(),
            sp.GetRequiredService<IDeployRepository>(),
            sp.GetRequiredService<ITagRepository>(),
            scope);
    }
}
