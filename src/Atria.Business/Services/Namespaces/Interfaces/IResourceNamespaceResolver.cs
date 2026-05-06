namespace Atria.Business.Services.Namespaces.Interfaces;

public interface IResourceNamespaceResolver
{
    string Resolve();

    Task<string> ResolveForFeedAsync(Guid feedId, CancellationToken ct);
}
