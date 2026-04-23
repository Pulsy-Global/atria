using Atria.Business.Models.Options;
using Atria.Common.KV.Interfaces;
using Microsoft.Extensions.Options;

namespace Atria.Business.Services.Deployment;

public class KvNamespaceResolver : IKvNamespaceResolver
{
    private readonly string _namespace;

    public KvNamespaceResolver(IOptions<KvOptions> options)
    {
        _namespace = options.Value.Namespace;
    }

    public string Resolve() => _namespace;
}
