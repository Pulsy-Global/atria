using Atria.Common.KV.Factory;
using Atria.Common.KV.Tests.Fakes;
using FluentAssertions;

namespace Atria.Common.KV.Tests;

public class KvStoreFactoryTests
{
    [Fact]
    public async Task CreateAsync_ReturnsMemoizedStorePerNamespace()
    {
        var client = new FakeEkvClient();
        var ns = new FakeEkvNamespace();
        client.SeedNamespace("workspace-1", ns);
        var factory = new KvStoreFactory(client);

        var first = await factory.CreateAsync("workspace-1");
        var second = await factory.CreateAsync("workspace-1");

        second.Should().BeSameAs(first);
        client.AdminStub.EnsureCalls.Should().Be(1, "EnsureNamespaceAsync is one admin call per namespace, not per request");
    }

    [Fact]
    public async Task CreateAsync_IsolatesNamespaces()
    {
        var client = new FakeEkvClient();
        client.SeedNamespace("workspace-1", new FakeEkvNamespace());
        client.SeedNamespace("workspace-2", new FakeEkvNamespace());
        var factory = new KvStoreFactory(client);

        var first = await factory.CreateAsync("workspace-1");
        var second = await factory.CreateAsync("workspace-2");

        second.Should().NotBeSameAs(first);
        client.AdminStub.EnsureCalls.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_TransientFailure_IsNotCached()
    {
        var client = new FakeFailingEkvClient();
        var factory = new KvStoreFactory(client);

        var act = () => factory.CreateAsync("workspace-1");
        await act.Should().ThrowAsync<InvalidOperationException>();

        client.Recover();
        var store = await factory.CreateAsync("workspace-1");

        store.Should().NotBeNull();
    }
}
