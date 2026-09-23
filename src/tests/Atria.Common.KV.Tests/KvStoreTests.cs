using Atria.Common.KV.Tests.Fakes;
using FluentAssertions;
using System.Text;

namespace Atria.Common.KV.Tests;

// These tests pin the cost model behind the bucket-list optimization:
// Cloud Atria bills physical (uncached) storage reads, and random reads over the
// large "B:" keyspace are the expensive shape. Writes must therefore be blind (no
// existence probes on data keys, no MultiGet), and the tiny cache-hot "R:" registry
// prefix — presence only, no per-bucket counts — is what lets us list buckets
// without ever scanning "B:". "B:" is only walked for a one-time legacy backfill.
public class KvStoreTests
{
    private const string RegistrySentinel = "R:";

    [Fact]
    public async Task BucketAddAsync_WritesDataAndMarkerInOneBatch_WithoutProbingDataKeys()
    {
        var ns = new FakeEkvNamespace();
        var store = new KvStore(ns);

        await store.BucketAddAsync("logs", "k1", "v1");

        ns.GetCalls.Should().Be(0, "blind writes never read the data key");
        ns.MultiGetCalls.Should().Be(0);
        ns.Batches.Should().ContainSingle("data and marker ride in a single batch");
        ReadString(ns, "B:logs:k1").Should().Be("v1");
        HasMarker(ns, "logs").Should().BeTrue();
    }

    [Fact]
    public async Task BucketAddAsync_RepeatedAdds_RemainReadFree()
    {
        var ns = new FakeEkvNamespace();
        var store = new KvStore(ns);

        await store.BucketAddAsync("logs", "k1", "v1");
        await store.BucketAddAsync("logs", "k2", "v2");

        ns.GetCalls.Should().Be(0, "there is no count to maintain, so adds never read");
        ReadString(ns, "B:logs:k2").Should().Be("v2");
    }

    [Fact]
    public async Task BucketAddBatchAsync_BlindWrite_WritesAllItemsAndASingleMarker()
    {
        var ns = new FakeEkvNamespace();
        var store = new KvStore(ns);

        await store.BucketAddBatchAsync("logs", new Dictionary<string, string>
        {
            ["k1"] = "v1",
            ["k2"] = "v2",
            ["k3"] = "v3",
        });

        ns.MultiGetCalls.Should().Be(0, "the existence MultiGet was the batch read cost");
        ns.GetCalls.Should().Be(0);
        ns.Batches.Should().ContainSingle();
        HasMarker(ns, "logs").Should().BeTrue();
        ns.Data.Should().ContainKeys("B:logs:k1", "B:logs:k2", "B:logs:k3");
    }

    [Fact]
    public async Task BucketRemoveAsync_BlindDelete_RemovesDataAndKeepsTheMarker()
    {
        var ns = new FakeEkvNamespace();
        var store = new KvStore(ns);

        await store.BucketAddAsync("logs", "k1", "v1");
        await store.BucketAddAsync("logs", "k2", "v2");
        ns.Batches.Clear();

        await store.BucketRemoveAsync("logs", "k1");

        ns.GetCalls.Should().Be(0, "the delete is blind: it never probes the data key");
        ns.Batches.Should().ContainSingle();
        ns.Data.Should().NotContainKey("B:logs:k1");
        HasMarker(ns, "logs").Should().BeTrue("a bucket may be repopulated, so the marker stays");
    }

    [Fact]
    public async Task BucketRemoveBatchAsync_BlindDelete_SingleBatchWithoutProbing()
    {
        var ns = new FakeEkvNamespace();
        var store = new KvStore(ns);

        await store.BucketAddBatchAsync("logs", new Dictionary<string, string>
        {
            ["k1"] = "v1",
            ["k2"] = "v2",
            ["k3"] = "v3",
        });
        ns.Batches.Clear();

        await store.BucketRemoveBatchAsync("logs", ["k1", "k2"]);

        ns.GetCalls.Should().Be(0);
        ns.Batches.Should().ContainSingle();
        ns.Data.Should().NotContainKeys("B:logs:k1", "B:logs:k2");
        ns.Data.Should().ContainKey("B:logs:k3");
    }

    [Fact]
    public async Task ListBucketsAsync_ScansRegistryAndReturnsNames()
    {
        var ns = new FakeEkvNamespace();
        SeedSentinel(ns);
        SeedMarker(ns, "a");
        SeedMarker(ns, "b");
        var store = new KvStore(ns);

        var result = await store.ListBucketsAsync(10);

        ns.GetCalls.Should().Be(1, "the sentinel presence check is the only read besides the scan");
        result.Names.Should().Equal("a", "b");
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task ListBucketsAsync_LegacyNamespace_BackfillsMarkersOnce()
    {
        var ns = new FakeEkvNamespace();
        SeedItem(ns, "x", "1");
        SeedItem(ns, "x", "2");
        SeedItem(ns, "y", "1");
        var store = new KvStore(ns);

        var result = await store.ListBucketsAsync(10);

        result.Names.Should().Equal("x", "y");
        HasMarker(ns, "x").Should().BeTrue();
        HasMarker(ns, "y").Should().BeTrue();
        HasSentinel(ns).Should().BeTrue("the sentinel records that the registry was seeded");

        ns.Batches.Clear();
        var getsBefore = ns.GetCalls;

        var second = await store.ListBucketsAsync(10);

        second.Names.Should().Equal("x", "y");
        ns.GetCalls.Should().Be(getsBefore, "the registry check is memoized per store");
        ns.Batches.Should().BeEmpty("backfill must not repeat");
    }

    private static string ReadString(FakeEkvNamespace ns, string key)
        => Encoding.UTF8.GetString(ns.Data[key]);

    private static bool HasMarker(FakeEkvNamespace ns, string bucket)
        => ns.Data.TryGetValue("R:" + bucket, out var value) && value.Length > 0;

    private static bool HasSentinel(FakeEkvNamespace ns)
        => ns.Data.TryGetValue(RegistrySentinel, out var value) && value.Length > 0;

    private static void SeedItem(FakeEkvNamespace ns, string bucket, string key)
        => ns.Seed($"B:{bucket}:{key}", Encoding.UTF8.GetBytes("v"));

    private static void SeedMarker(FakeEkvNamespace ns, string bucket)
        => ns.Seed("R:" + bucket, [1]);

    private static void SeedSentinel(FakeEkvNamespace ns)
        => ns.Seed(RegistrySentinel, [1]);
}
