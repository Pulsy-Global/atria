using Atria.Business.Services.Namespaces.Interfaces;
using Atria.Common.KV.Factory;
using Atria.Common.KV.Interfaces;
using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Kv;
using MapsterMapper;
using Microsoft.Extensions.Logging;

namespace Atria.Core.Business.Managers;

public class KvManager(
    IKvStoreFactory kvStoreFactory,
    IResourceNamespaceResolver resourceNamespaceResolver,
    ILogger<KvManager> logger,
    IMapper mapper)
    : BaseManager(logger, mapper), IKvManager
{
    private readonly Task<IKvStore> _kvStoreTask =
        kvStoreFactory.CreateAsync(resourceNamespaceResolver.Resolve());

    public async Task AddBucketAsync(string bucket, BucketItemDto dto)
    {
        var kvStore = await GetKvStoreAsync();
        await kvStore.BucketAddAsync(bucket, dto.Key, dto.Value);
    }

    public async Task<BucketValueDto> GetBucketValueAsync(string bucket, string key)
    {
        var kvStore = await GetKvStoreAsync();
        var value = await kvStore.BucketGetAsync(bucket, key);
        return new BucketValueDto { Value = value };
    }

    public async Task DeleteBucketAsync(string bucket, string key)
    {
        var kvStore = await GetKvStoreAsync();
        await kvStore.BucketRemoveAsync(bucket, key);
    }

    public async Task<BucketValuesDto> GetBucketValuesAsync(
        string bucket,
        int limit,
        string? cursor = null)
    {
        var kvStore = await GetKvStoreAsync();
        var result = await kvStore.BucketValuesAsync(bucket, limit, cursor);
        return Mapper.Map<BucketValuesDto>(result);
    }

    public async Task AddBucketBatchAsync(string bucket, AddBucketBatchDto dto)
    {
        var kvStore = await GetKvStoreAsync();
        await kvStore.BucketAddBatchAsync(bucket, dto.ToReadOnlyItems());
    }

    public async Task DeleteBucketBatchAsync(string bucket, BucketBatchKeysDto dto)
    {
        var kvStore = await GetKvStoreAsync();
        await kvStore.BucketRemoveBatchAsync(bucket, dto.Keys);
    }

    public async Task<BucketBatchItemsDto> GetBucketBatchAsync(string bucket, BucketBatchKeysDto dto)
    {
        var kvStore = await GetKvStoreAsync();
        var items = await kvStore.BucketGetBatchAsync(bucket, dto.Keys);
        return new BucketBatchItemsDto { Items = items };
    }

    private async Task<IKvStore> GetKvStoreAsync()
        => await _kvStoreTask;
}
