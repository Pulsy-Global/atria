using Atria.Common.KV.Tests.Fakes;
using FluentAssertions;
using System.Text;

namespace Atria.Common.KV.Tests;

public class KvStoreTests
{
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
        SeedMarker(ns, "a");
        SeedMarker(ns, "b");
        var store = new KvStore(ns);

        var result = await store.ListBucketsAsync(10);

        ns.GetCalls.Should().Be(0, "there is nothing to probe, the registry scan is the whole listing");
        ns.ScanCalls.Should().Be(1, "only the R: page is read");
        result.Names.Should().Equal("a", "b");
        result.Total.Should().Be(2);
    }

    [Fact]
    public async Task ListBucketsAsync_RegistryEmpty_DoesNotScanDataKeysOrCreateMarkers()
    {
        var ns = new FakeEkvNamespace();
        SeedItem(ns, "x", "1");
        SeedItem(ns, "y", "1");
        var store = new KvStore(ns);

        var result = await store.ListBucketsAsync(10);

        result.Names.Should().BeEmpty("unregistered buckets stay invisible, there is no backfill");
        ns.ScanCalls.Should().Be(1, "listing never falls back to scanning B: keys");
        ns.Batches.Should().BeEmpty("nothing gets backfilled");
        HasMarker(ns, "x").Should().BeFalse();
        HasMarker(ns, "y").Should().BeFalse();
    }

    [Fact]
    public async Task BucketAddAsync_SameBucketTwice_WritesMarkerOnce()
    {
        var ns = new FakeEkvNamespace();
        var store = new KvStore(ns);

        await store.BucketAddAsync("logs", "k1", "v1");
        ns.Batches.Clear();

        await store.BucketAddAsync("logs", "k2", "v2");

        ns.Batches.Should().ContainSingle();
        ns.Batches[0].Should().HaveCount(1, "the marker is remembered in memory and written once");
        ReadString(ns, "B:logs:k2").Should().Be("v2");
        HasMarker(ns, "logs").Should().BeTrue();
    }

    [Fact]
    public async Task BucketAddBatchAsync_BucketAlreadyRegistered_SkipsTheMarker()
    {
        var ns = new FakeEkvNamespace();
        var store = new KvStore(ns);

        await store.BucketAddAsync("logs", "k1", "v1");
        ns.Batches.Clear();

        await store.BucketAddBatchAsync("logs", new Dictionary<string, string>
        {
            ["k2"] = "v2",
            ["k3"] = "v3",
        });

        ns.Batches.Should().ContainSingle();
        ns.Batches[0].Should().HaveCount(2, "two items and no marker");
        ns.Data.Should().ContainKeys("B:logs:k2", "B:logs:k3");
    }

    [Fact]
    public async Task BucketAddAsync_AfterListing_SkipsTheMarkerOfAKnownBucket()
    {
        var ns = new FakeEkvNamespace();
        SeedMarker(ns, "logs");
        var store = new KvStore(ns);

        await store.ListBucketsAsync(10);
        var batchesBefore = ns.Batches.Count;

        await store.BucketAddAsync("logs", "k1", "v1");

        ns.Batches.Should().HaveCount(batchesBefore + 1);
        ns.Batches[^1].Should().HaveCount(1, "the listing already proved the marker exists");
        ReadString(ns, "B:logs:k1").Should().Be("v1");
    }

    [Fact]
    public async Task BucketAddAsync_FreshStoreInstance_WritesMarker()
    {
        var ns = new FakeEkvNamespace();

        await new KvStore(ns).BucketAddAsync("logs", "k1", "v1");
        ns.Batches.Clear();

        await new KvStore(ns).BucketAddAsync("logs", "k2", "v2");

        ns.Batches[0].Should().HaveCount(2, "the shortcut is per instance, so lost state costs one idempotent put");
        HasMarker(ns, "logs").Should().BeTrue();
    }

    private static string ReadString(FakeEkvNamespace ns, string key)
        => Encoding.UTF8.GetString(ns.Data[key]);

    private static bool HasMarker(FakeEkvNamespace ns, string bucket)
        => ns.Data.TryGetValue("R:" + bucket, out var value) && value.Length > 0;

    private static void SeedItem(FakeEkvNamespace ns, string bucket, string key)
        => ns.Seed($"B:{bucket}:{key}", Encoding.UTF8.GetBytes("v"));

    private static void SeedMarker(FakeEkvNamespace ns, string bucket)
        => ns.Seed("R:" + bucket, [1]);
}
