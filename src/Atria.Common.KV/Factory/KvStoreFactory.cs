using Atria.Common.KV.Interfaces;
using Pulsy.EKV.Client;
using Pulsy.EKV.Client.Models;

namespace Atria.Common.KV.Factory;

public class KvStoreFactory : IKvStoreFactory
{
    private readonly IEkvClient _ekvClient;

    public KvStoreFactory(IEkvClient ekvClient)
    {
        _ekvClient = ekvClient;
    }

    public async Task<IKvStore> CreateAsync(string namespaceName)
    {
        var admin = _ekvClient.Admin();
        await admin.EnsureNamespaceAsync(new NamespaceInfo { Name = namespaceName });

        return new KvStore(_ekvClient.Namespace(namespaceName));
    }
}
