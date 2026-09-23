using Pulsy.EKV.Client;
using Pulsy.EKV.Client.Admin;
using Pulsy.EKV.Client.Models;
using Pulsy.EKV.Client.Namespaces;

namespace Atria.Common.KV.Tests.Fakes;

internal sealed class FakeEkvClient : IEkvClient
{
    private readonly Dictionary<string, IEkvNamespace> _namespaces = new(StringComparer.Ordinal);

    public FakeEkvAdmin AdminStub { get; } = new();

    public IEkvNamespace Namespace(string name)
        => _namespaces.TryGetValue(name, out var ns)
            ? ns
            : throw new InvalidOperationException($"Namespace '{name}' was not seeded.");

    public IEkvAdmin Admin() => AdminStub;

    public void Dispose()
    {
        // Nothing to release: fully in-memory.
    }

    public void SeedNamespace(string name, IEkvNamespace ns) => _namespaces[name] = ns;
}

internal sealed class FakeEkvAdmin : IEkvAdmin
{
    public bool FailEnsure { get; set; }

    public int EnsureCalls { get; private set; }

    public Task EnsureNamespaceAsync(NamespaceInfo config, CancellationToken ct = default)
    {
        if (FailEnsure)
        {
            throw new InvalidOperationException("ekv node unavailable");
        }

        EnsureCalls++;
        return Task.CompletedTask;
    }

    public Task CreateNamespaceAsync(NamespaceInfo config, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<NamespaceInfo?> GetNamespaceAsync(string name, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<NamespaceInfo>> ListNamespacesAsync(CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task UpdateNamespaceAsync(NamespaceInfo config, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task HibernateNamespaceAsync(string name, CancellationToken ct = default)
        => throw new NotSupportedException();

    public Task DeleteNamespaceAsync(string name, CancellationToken ct = default)
        => throw new NotSupportedException();
}
