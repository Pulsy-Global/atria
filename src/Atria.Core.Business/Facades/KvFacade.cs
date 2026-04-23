using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Kv;

namespace Atria.Core.Business.Facades;

public class KvFacade(IKvManager kvManager)
{
    public async Task AddBucketAsync(string bucket, BucketItemDto dto)
        => await kvManager.AddBucketAsync(bucket, dto);

    public async Task<BucketValueDto> GetBucketValueAsync(string bucket, string key)
        => await kvManager.GetBucketValueAsync(bucket, key);

    public async Task DeleteBucketAsync(string bucket, string key)
        => await kvManager.DeleteBucketAsync(bucket, key);

    public async Task<BucketValuesDto> GetBucketValuesAsync(
        string bucket,
        int limit,
        string? cursor = null)
        => await kvManager.GetBucketValuesAsync(bucket, limit, cursor);

    public async Task AddBucketBatchAsync(string bucket, AddBucketBatchDto dto)
        => await kvManager.AddBucketBatchAsync(bucket, dto);

    public async Task DeleteBucketBatchAsync(string bucket, BucketBatchKeysDto dto)
        => await kvManager.DeleteBucketBatchAsync(bucket, dto);

    public async Task<BucketBatchItemsDto> GetBucketBatchAsync(string bucket, BucketBatchKeysDto dto)
        => await kvManager.GetBucketBatchAsync(bucket, dto);
}
