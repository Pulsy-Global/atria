using Atria.Business.Services.DataServices.Interfaces;
using Atria.Business.Services.Deployment.Interfaces;
using Atria.Common.Exceptions;
using Atria.Common.Models.Generic;
using Atria.Core.Data.Entities.Constants;
using Atria.Core.Data.Entities.Outputs;
using Atria.Core.Data.Entities.Outputs.Config;
using Atria.Core.Data.Extensions;
using Atria.Core.Data.Models.Query;
using Atria.Core.Data.UnitOfWork.Context;
using Atria.Core.Data.UnitOfWork.Factory;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Atria.Business.Services.DataServices;

public class OutputDataService : IOutputDataService
{
    private readonly IUnitOfWorkFactory _unitOfWorkFactory;
    private readonly IOutputEventPublisher _outputEventPublisher;
    private readonly ILogger<FeedDataService> _logger;

    public OutputDataService(
        IUnitOfWorkFactory unitOfWorkFactory,
        IOutputEventPublisher outputEventPublisher,
        ILogger<FeedDataService> logger)
    {
        _unitOfWorkFactory = unitOfWorkFactory;
        _outputEventPublisher = outputEventPublisher;
        _logger = logger;
    }

    public async Task<Output> CreateOutputAsync(Output entity, CancellationToken ct, List<Guid>? tagIds = null)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        await UpdateOutputTagsAsync(entity, tagIds, uow, ct);

        uow.OutputRepository.Create(entity);

        await uow.SaveChangesAsync(ct);

        return entity;
    }

    public async Task<Output> UpdateOutputAsync(Output entity, CancellationToken ct, List<Guid>? tagIds = null)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        await UpdateOutputTagsAsync(entity, tagIds, uow, ct);

        uow.OutputRepository.Update(entity);

        await uow.SaveChangesAsync(ct);

        await _outputEventPublisher.PublishOutputUpdatedAsync(entity.Id, ct);

        return entity;
    }

    public async Task<Output> GetOutputByIdAsync(Guid id, CancellationToken ct, params Expression<Func<Output, object>>[] includes)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entity = await uow.OutputRepository.GetAsync(x => x.Id == id, ct, includes);

        if (entity == null)
        {
            throw new ItemNotFoundException($"Output with id {id} not found");
        }

        return entity;
    }

    public async Task<List<Output>> GetOutputsAsync(Expression<Func<Output, bool>> predicate, CancellationToken ct, params Expression<Func<Output, object>>[] includes)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entities = await uow.OutputRepository.GetListAsync(predicate, ct, ignoreFilters: false, includes);

        return entities;
    }

    public async Task<PagedList<Output>> GetOutputsAsync(QueryOptions<Output> queryOptions, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entities = await uow.OutputRepository.GetOutputsAsync(queryOptions, ct);

        return entities;
    }

    public async Task DeleteOutputAsync(Guid id, CancellationToken ct)
    {
        using var uow = _unitOfWorkFactory.BuildContext();

        var entity = await GetOutputByIdAsync(id, ct);

        var linkedFeeds = await uow.FeedOutputRepository.ExistsAsync(
            fo => fo.OutputId == id && fo.Feed.DeletedAt == null, ct);

        if (linkedFeeds)
        {
            throw new ValidationException(
                $"Cannot delete output '{entity.Name}': it is linked to feed(s). Remove it from all feeds first.");
        }

        uow.OutputRepository.Delete(entity);

        await uow.SaveChangesAsync(ct);
    }

    public TConfig? GetTypedConfig<TConfig>(Output output)
        where TConfig : OutputConfigBase
    {
        return output.Config as TConfig;
    }

    private async Task UpdateOutputTagsAsync(
        Output output,
        IEnumerable<Guid>? tagIds,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var uniqueTagIds = tagIds?.Distinct().ToList();

        if (uniqueTagIds != null)
        {
            var existingTags = await uow.TagRepository.GetListAsync(
                x => uniqueTagIds.Contains(x.Id) && x.Type == TagType.Output, ct);

            var existingTagIds = existingTags
                .Select(x => x.Id)
                .ToHashSet();

            var missingTagIds = uniqueTagIds
                .Where(id => !existingTagIds.Contains(id))
                .ToList();

            if (missingTagIds.Any())
            {
                throw new ItemNotFoundException(
                    $"Output tag(s) with id(s) [{string.Join(", ", missingTagIds)}] not found");
            }
        }

        var (toRemove, toAdd) = output.UpdateOutputTags(uniqueTagIds);

        foreach (var item in toRemove)
        {
            uow.OutputTagRepository.Delete(item);
        }

        foreach (var item in toAdd)
        {
            uow.OutputTagRepository.Create(item);
        }
    }
}
