using Atria.Common.Exceptions;
using Atria.Core.Business.Managers.Interfaces;
using Atria.Core.Business.Models.Dto.Tag;
using Atria.Core.Data.Entities.Tags;
using Atria.Core.Data.UnitOfWork.Factory;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Atria.Core.Business.Managers;

public class TagManager : BaseManager, ITagManager
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;

    public TagManager(
        IUnitOfWorkFactory unitOfWorkFactory,
        ILogger<TagManager> logger,
        IMapper mapper)
        : base(logger, mapper)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
    }

    public async Task<TagDto> CreateTagAsync(CreateTagDto dto, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entity = Mapper.Map<Tag>(dto);

        await uow.TagRepository.CreateAsync(entity, ct);

        await uow.SaveChangesAsync(ct);

        return Mapper.Map<TagDto>(entity);
    }

    public async Task<TagDto> UpdateTagAsync(Guid id, UpdateTagDto dto, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entity = await uow.TagRepository.GetAsync(id, ct);

        if (entity == null)
        {
            throw new ItemNotFoundException($"Tag with id {id} not found");
        }

        Mapper.Map(dto, entity);

        uow.TagRepository.Update(entity);

        await uow.SaveChangesAsync(ct);

        return Mapper.Map<TagDto>(entity);
    }

    public async Task<TagDto> GetTagAsync(Guid id, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entity = await uow.TagRepository.GetAsync(id, ct);

        if (entity == null)
        {
            throw new ItemNotFoundException($"Tag with id {id} not found");
        }

        return Mapper.Map<TagDto>(entity);
    }

    public async Task<List<TagDto>> GetTagsByTypeAsync(string type, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entities = await uow.TagRepository.GetTagsByTypeAsync(type, ct);

        return Mapper.Map<List<TagDto>>(entities);
    }

    public async Task<List<TagDto>> GetTagsAsync(
        Expression<Func<Tag, bool>> predicate,
        CancellationToken ct,
        params Expression<Func<Tag, object>>[] includes)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entities = await uow.TagRepository
            .GetListAsync(predicate, ct, ignoreFilters: false, includes);

        return Mapper.Map<List<TagDto>>(entities);
    }

    public async Task DeleteTagAsync(Guid id, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entity = await uow.TagRepository.GetAsync(id, ct);

        if (entity == null)
        {
            throw new ItemNotFoundException($"Tag with id {id} not found");
        }

        uow.TagRepository.Delete(entity);

        await uow.SaveChangesAsync(ct);
    }
}
