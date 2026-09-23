using Atria.Common.KV.Interfaces;
using Pulsy.EKV.Client;
using Pulsy.EKV.Client.Models;
using System.Collections.Concurrent;

namespace Atria.Common.KV.Factory;

public class KvStoreFactory : IKvStoreFactory
{
    private readonly IEkvClient _ekvClient;

    private readonly ConcurrentDictionary<string, Task<IKvStore>> _stores = new(StringComparer.Ordinal);

    public KvStoreFactory(IEkvClient ekvClient)
    {
        _ekvClient = ekvClient;
    }

    public Task<IKvStore> CreateAsync(string namespaceName)
    {
        var task = _stores.GetOrAdd(namespaceName, CreateStoreAsync);

        // A cached faulted task must not poison the namespace for the process
        // lifetime: drop it and retry once. If the retry also fails, the caller
        // sees the error and the next call gets a fresh attempt.
        if (task.IsFaulted || task.IsCanceled)
        {
            _stores.TryRemove(new KeyValuePair<string, Task<IKvStore>>(namespaceName, task));
            task = _stores.GetOrAdd(namespaceName, CreateStoreAsync);
        }

        return task;
    }

    private async Task<IKvStore> CreateStoreAsync(string namespaceName)
    {
        var admin = _ekvClient.Admin();
        await admin.EnsureNamespaceAsync(new NamespaceInfo { Name = namespaceName });

        return new KvStore(_ekvClient.Namespace(namespaceName));
    }
}
