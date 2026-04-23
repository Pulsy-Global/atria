using Atria.Business.Services.Storage.Interfaces;
using Atria.Orchestrator.Models.Business;
using Atria.Orchestrator.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atria.Orchestrator.Services.Deployment;

public class ManifestScanner : IManifestScanner
{
    private readonly IFileSystemService _fileStorageService;
    private readonly ILogger<ManifestScanner> _logger;

    public ManifestScanner(
        IFileSystemService fileStorageService,
        ILogger<ManifestScanner> logger)
    {
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public bool HasChanges(ScanResult<FeedDeployment> scanResult)
    {
        return scanResult.Added.Any() ||
               scanResult.Modified.Any() ||
               scanResult.RemovedIds.Any();
    }

    public async Task<List<DirectoryScanResult<TResult>>> ScanDirectoryAsync<TResult>(
        string directoryPath,
        string fileName,
        Func<TResult, bool>? validate = null)
        where TResult : class
    {
        var results = new List<DirectoryScanResult<TResult>>();

        if (!await _fileStorageService.DirectoryExistsAsync(directoryPath))
        {
            _logger.LogWarning("Directory does not exist: {DirectoryPath}", directoryPath);
            return results;
        }

        var directories = await _fileStorageService.ListDirectoriesAsync(directoryPath);

        foreach (var dir in directories)
        {
            try
            {
                var directoryName = Path.GetFileName(dir);

                var manifestPath = Path.Combine(dir, fileName);

                if (await _fileStorageService.FileExistsAsync(manifestPath))
                {
                    var stream = await _fileStorageService.GetFileAsync(manifestPath);
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();

                    var fileHash = ComputeFileHash(content);

                    var item = TryParse<TResult>(content);

                    if (item != null && (validate == null || validate(item)))
                    {
                        var scanResult = new DirectoryScanResult<TResult>
                        {
                            DirectoryName = directoryName,
                            FileHash = fileHash,
                            Item = item,
                        };
                        results.Add(scanResult);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process directory: {DirectoryPath}", dir);
            }
        }

        return results;
    }

    private TResult? TryParse<TResult>(string content)
        where TResult : class
    {
        try
        {
            return JsonSerializer.Deserialize<TResult>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    Converters = { new JsonStringEnumConverter() },
                });
        }
        catch (JsonException e)
        {
            _logger.LogError("Manifest deserialization error: {Error}", e.Message);
            return null;
        }
    }

    private string ComputeFileHash(string content)
    {
        var data = Encoding.UTF8.GetBytes(content);
        var hash = XxHash32.Hash(data);

        return Convert.ToHexString(hash);
    }
}
