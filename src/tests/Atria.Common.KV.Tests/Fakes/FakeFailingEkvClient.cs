using Pulsy.EKV.Client;
using Pulsy.EKV.Client.Admin;
using Pulsy.EKV.Client.Namespaces;

namespace Atria.Common.KV.Tests.Fakes;

// Starts with EnsureNamespaceAsync failing (simulating a cold admin-call hiccup)
// so a faulted store cannot stay cached; Recover() makes the namespace usable.
internal sealed class FakeFailingEkvClient : IEkvClient
{
    private readonly FakeEkvClient _inner = new();

    public FakeFailingEkvClient() => _inner.AdminStub.FailEnsure = true;

    public void Recover()
    {
        _inner.AdminStub.FailEnsure = false;
        _inner.SeedNamespace("workspace-1", new FakeEkvNamespace());
    }

    public IEkvNamespace Namespace(string name) => _inner.Namespace(name);

    public IEkvAdmin Admin() => _inner.Admin();

    public void Dispose() => _inner.Dispose();
}
