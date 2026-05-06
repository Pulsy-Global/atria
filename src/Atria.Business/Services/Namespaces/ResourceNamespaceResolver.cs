using Atria.Business.Models.Options;
using Atria.Business.Services.Namespaces.Interfaces;
using Microsoft.Extensions.Options;

namespace Atria.Business.Services.Namespaces;

public class ResourceNamespaceResolver : IResourceNamespaceResolver
{
    private readonly string _namespace;

    public ResourceNamespaceResolver(IOptions<KvOptions> options)
    {
        _namespace = options.Value.Namespace;
    }

    public string Resolve()
    {
        if (string.IsNullOrWhiteSpace(_namespace))
        {
            throw new InvalidOperationException("Resource namespace is not available.");
        }

        if (_namespace is "." or ".." || _namespace.Contains('/') || _namespace.Contains('\\'))
        {
            throw new InvalidOperationException("Resource namespace contains invalid path characters.");
        }

        return _namespace;
    }

    public Task<string> ResolveForFeedAsync(Guid feedId, CancellationToken ct)
        => Task.FromResult(Resolve());
}
