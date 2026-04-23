using Atria.Core.Business.Models.Dto.Tag;
using Atria.Core.Data.Entities.Tags;
using System.Linq.Expressions;

namespace Atria.Core.Business.Managers.Interfaces;

public interface ITagManager : IBaseManager
{
    Task<TagDto> CreateTagAsync(CreateTagDto dto, CancellationToken ct);
    Task<TagDto> UpdateTagAsync(Guid id, UpdateTagDto dto, CancellationToken ct);
    Task<TagDto> GetTagAsync(Guid id, CancellationToken ct);
    Task<List<TagDto>> GetTagsByTypeAsync(string type, CancellationToken ct);

    Task<List<TagDto>> GetTagsAsync(
        Expression<Func<Tag, bool>> predicate,
        CancellationToken ct,
        params Expression<Func<Tag, object>>[] includes);

    Task DeleteTagAsync(Guid id, CancellationToken ct);
}
