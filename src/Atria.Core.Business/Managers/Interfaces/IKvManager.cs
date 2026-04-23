using Atria.Core.Business.Models.Dto.Kv;

namespace Atria.Core.Business.Managers.Interfaces;

public interface IKvManager
{
    Task AddBucketAsync(string bucket, BucketItemDto dto);

    Task<BucketValueDto> GetBucketValueAsync(string bucket, string key);

    Task DeleteBucketAsync(string bucket, string key);

    Task<BucketValuesDto> GetBucketValuesAsync(string bucket, int limit, string? cursor = null);

    Task AddBucketBatchAsync(string bucket, AddBucketBatchDto dto);

    Task DeleteBucketBatchAsync(string bucket, BucketBatchKeysDto dto);

    Task<BucketBatchItemsDto> GetBucketBatchAsync(string bucket, BucketBatchKeysDto dto);
}
