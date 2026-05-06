using Atria.Business.Models.Enums;
using Atria.Common.Models.Generic;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Models.Query;
using System.Linq.Expressions;

namespace Atria.Business.Services.DataServices.Interfaces;

public interface IFeedDataService
{
    Task<Feed> CreateFeedAsync(
        Feed entity,
        CancellationToken ct,
        List<Guid>? outputIds = null,
        List<Guid>? tagIds = null);

    Task<Feed> UpdateFeedAsync(
        Feed entity,
        CancellationToken ct,
        List<Guid>? outputIds = null,
        List<Guid>? tagIds = null);

    Task<Feed> GetFeedByIdAsync(Guid id, CancellationToken ct, params Expression<Func<Feed, object>>[] includes);

    Task<List<Feed>> GetFeedsAsync(Expression<Func<Feed, bool>> predicate, CancellationToken ct, params Expression<Func<Feed, object>>[] includes);

    Task<PagedList<Feed>> GetFeedsAsync(QueryOptions<Feed>? queryOptions, CancellationToken ct);

    Task DeleteFeedAsync(Guid id, CancellationToken ct);

    Task<string?> GetFeedFileAsync(Guid id, FeedFileType type, CancellationToken ct);

    Task DeleteFeedFileAsync(Feed feed, FeedFileType type, CancellationToken ct);

    Task<string?> UploadFeedFileAsync(Feed feed, FeedFileType type, string content, CancellationToken ct);
}
