using Atria.Business.Models.Enums;
using Atria.Business.Models.Options;
using Atria.Business.Services.DataServices.Interfaces;
using Atria.Business.Services.Storage.Interfaces;
using Atria.Common.Exceptions;
using Atria.Common.Models.Generic;
using Atria.Core.Data.Entities.Constants;
using Atria.Core.Data.Entities.Feeds;
using Atria.Core.Data.Extensions;
using Atria.Core.Data.Models.Query;
using Atria.Core.Data.UnitOfWork.Context;
using Atria.Core.Data.UnitOfWork.Factory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq.Expressions;

namespace Atria.Business.Services.DataServices;

public class FeedDataService(
    IFileSystemService fileStorageService,
    IOptions<FileStorageOptions> fileStorageOptions,
    IUnitOfWorkFactory unitOfWorkFactory,
    ILogger<FeedDataService> logger)
    : IFeedDataService
{
    private readonly FileStorageOptions _fileStorageOptions = fileStorageOptions.Value;

    public async Task<Feed> CreateFeedAsync(
        Feed entity,
        CancellationToken ct,
        List<Guid>? outputIds = null,
        List<Guid>? tagIds = null)
    {
        using var uow = unitOfWorkFactory.BuildContext();

        await UpdateFeedOutputsAsync(entity, outputIds, uow, ct);
        await UpdateFeedTagsAsync(entity, tagIds, uow, ct);

        uow.FeedRepository.Create(entity);

        await uow.SaveChangesAsync(ct);

        return entity;
    }

    public async Task<Feed> UpdateFeedAsync(
        Feed entity,
        CancellationToken ct,
        List<Guid>? outputIds = null,
        List<Guid>? tagIds = null)
    {
        using var uow = unitOfWorkFactory.BuildContext();

        await UpdateFeedOutputsAsync(entity, outputIds, uow, ct);
        await UpdateFeedTagsAsync(entity, tagIds, uow, ct);

        uow.FeedRepository.Update(entity);

        await uow.SaveChangesAsync(ct);

        return entity;
    }

    public async Task<Feed> GetFeedByIdAsync(Guid id, CancellationToken ct, params Expression<Func<Feed, object>>[] includes)
    {
        using var uow = unitOfWorkFactory.BuildContext();

        var entity = await uow.FeedRepository.GetAsync(x => x.Id == id, ct, includes);

        if (entity == null)
        {
            throw new ItemNotFoundException($"Feed with id {id} not found");
        }

        return entity;
    }

    public async Task<List<Feed>> GetFeedsAsync(Expression<Func<Feed, bool>> predicate, CancellationToken ct, params Expression<Func<Feed, object>>[] includes)
    {
        using var uow = unitOfWorkFactory.BuildContext();

        var entities = await uow.FeedRepository.GetListAsync(predicate, ct, ignoreFilters: false, includes);

        return entities;
    }

    public async Task<PagedList<Feed>> GetFeedsAsync(QueryOptions<Feed>? queryOptions, CancellationToken ct)
    {
        using var uow = unitOfWorkFactory.BuildContext();

        var entities = await uow.FeedRepository.GetFeedsAsync(queryOptions, ct);

        return entities;
    }

    public async Task DeleteFeedAsync(Guid id, CancellationToken ct)
    {
        using var uow = unitOfWorkFactory.BuildContext();

        var entity = await uow.FeedRepository.GetAsync(x => x.Id == id, ct);

        if (entity == null)
        {
            throw new ItemNotFoundException($"Feed with id {id} not found");
        }

        if (!string.IsNullOrEmpty(entity.FilterPath))
        {
            try
            {
                await fileStorageService.DeleteFileAsync(entity.FilterPath, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete filter file: {FilterPath}", entity.FilterPath);
            }
        }

        if (!string.IsNullOrEmpty(entity.FunctionPath))
        {
            try
            {
                await fileStorageService.DeleteFileAsync(entity.FunctionPath, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete function file: {FunctionPath}", entity.FunctionPath);
            }
        }

        uow.FeedRepository.Delete(entity);

        await uow.SaveChangesAsync(ct);
    }

    public Task DeleteFeedFileAsync(Guid id, FeedFileType type, CancellationToken ct)
    {
        var filePath = GenerateFilePath(id, type, ".js");
        return fileStorageService.DeleteFileAsync(filePath, ct);
    }

    public async Task<string?> UploadFeedFileAsync(Guid id, FeedFileType type, string content, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(content))
        {
            var filePath = GenerateFilePath(id, type, ".js");
            return await UploadFileAsync(content, filePath, ct);
        }

        return null;
    }

    public async Task<string?> GetFeedFileAsync(Guid id, FeedFileType type, CancellationToken ct)
    {
        using var uow = unitOfWorkFactory.BuildContext();

        var entity = await GetFeedByIdAsync(id, ct);

        var path = type == FeedFileType.Filter ? entity.FilterPath : entity.FunctionPath;

        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        if (!await fileStorageService.FileExistsAsync(path, ct))
        {
            throw new ItemNotFoundException($"File {entity.FilterPath} does not exist");
        }

        return await fileStorageService.ReadFileAsStringAsync(path, ct);
    }

    private async Task<string> UploadFileAsync(string content, string filePath, CancellationToken ct)
    {
        if (await fileStorageService.FileExistsAsync(filePath, ct))
        {
            try
            {
                await fileStorageService.DeleteFileAsync(filePath, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to delete existing file: {FilePath}", filePath);
            }
        }

        using var stream = new MemoryStream();

        await using var writer = new StreamWriter(stream);

        await writer.WriteAsync(content);
        await writer.FlushAsync(ct);

        stream.Position = 0;

        var uploadedPath = await fileStorageService
            .SaveFileAsync(filePath, stream, ct);

        return uploadedPath;
    }

    private async Task UpdateFeedOutputsAsync(
        Feed feed,
        IEnumerable<Guid>? outputIds,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var uniqueOutputIds = outputIds?.Distinct().ToList();

        if (uniqueOutputIds != null)
        {
            var existingOutputs = await uow.OutputRepository.GetListAsync(
                x => uniqueOutputIds.Contains(x.Id), ct);

            var existingOutputIds = existingOutputs
                .Select(x => x.Id)
                .ToHashSet();

            var missingOutputIds = uniqueOutputIds
                .Where(id => !existingOutputIds.Contains(id))
                .ToList();

            if (missingOutputIds.Any())
            {
                throw new ItemNotFoundException(
                    $"Output(s) with id(s) [{string.Join(", ", missingOutputIds)}] not found");
            }
        }

        var (toRemove, toAdd) = feed.UpdateFeedOutputs(uniqueOutputIds);

        foreach (var item in toRemove)
        {
            uow.FeedOutputRepository.Delete(item);
        }

        foreach (var item in toAdd)
        {
            uow.FeedOutputRepository.Create(item);
        }
    }

    private async Task UpdateFeedTagsAsync(
        Feed feed,
        IEnumerable<Guid>? tagIds,
        IUnitOfWork uow,
        CancellationToken ct)
    {
        var uniqueTagIds = tagIds?.Distinct().ToList();

        if (uniqueTagIds != null)
        {
            var existingTags = await uow.TagRepository.GetListAsync(
                x => uniqueTagIds.Contains(x.Id) && x.Type == TagType.Feed, ct);

            var existingTagIds = existingTags
                .Select(x => x.Id)
                .ToHashSet();

            var missingTagIds = uniqueTagIds
                .Where(id => !existingTagIds.Contains(id))
                .ToList();

            if (missingTagIds.Any())
            {
                throw new ItemNotFoundException(
                    $"Feed tag(s) with id(s) [{string.Join(", ", missingTagIds)}] not found");
            }
        }

        var (toRemove, toAdd) = feed.UpdateFeedTags(uniqueTagIds);

        foreach (var item in toRemove)
        {
            uow.FeedTagRepository.Delete(item);
        }

        foreach (var item in toAdd)
        {
            uow.FeedTagRepository.Create(item);
        }
    }

    private string GenerateFilePath(Guid feedId, FeedFileType fileType, string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        var fileName = $"{feedId}_{fileType}{extension}";

        string basePath = fileType switch
        {
            FeedFileType.Filter => Path.Combine(_fileStorageOptions.UploadsPath, _fileStorageOptions.FilterPath),
            FeedFileType.Function => Path.Combine(_fileStorageOptions.UploadsPath, _fileStorageOptions.FunctionPath),
            _ => throw new ArgumentException($"Unknown fileType: {fileType}")
        };

        return Path.Combine(basePath, fileName);
    }
}
