using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Tag;
using Atria.Core.Data.Entities.Tags;
using System.Linq.Expressions;

namespace Atria.Core.Business.Facades;

public class TagFacade
{
    private readonly ITagManager _tagManager;

    public TagFacade(ITagManager tagManager)
    {
        _tagManager = tagManager;
    }

    public async Task<TagDto> CreateTagAsync(CreateTagDto dto, CancellationToken ct) =>
        await _tagManager.CreateTagAsync(dto, ct);

    public async Task<TagDto> UpdateTagAsync(Guid id, UpdateTagDto dto, CancellationToken ct) =>
        await _tagManager.UpdateTagAsync(id, dto, ct);

    public async Task<TagDto> GetTagAsync(Guid id, CancellationToken ct) =>
        await _tagManager.GetTagAsync(id, ct);

    public async Task<List<TagDto>> GetTagsByTypeAsync(string type, CancellationToken ct) =>
        await _tagManager.GetTagsByTypeAsync(type, ct);

    public async Task<List<TagDto>> GetTagsAsync(
        Expression<Func<Tag, bool>> predicate,
        CancellationToken ct,
        params Expression<Func<Tag, object>>[] includes) => await _tagManager.GetTagsAsync(predicate, ct, includes);

    public async Task DeleteTagAsync(Guid id, CancellationToken ct) =>
        await _tagManager.DeleteTagAsync(id, ct);
}
