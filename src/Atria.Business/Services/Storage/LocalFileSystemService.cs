using Atria.Business.Models.Options;
using Atria.Business.Services.Storage.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Atria.Business.Services.Storage;

public class LocalFileSystemService : IFileSystemService
{
    private readonly LocalStorageOptions _options;
    private readonly ILogger<LocalFileSystemService> _logger;

    public LocalFileSystemService(
        IOptions<LocalStorageOptions> options,
        ILogger<LocalFileSystemService> logger)
    {
        _options = options.Value;
        _logger = logger;

        EnsureBaseDirectoryExists();
    }

    public async Task<string> SaveFileAsync(string filePath, Stream fileStream, CancellationToken ct = default)
    {
        var fullPath = GetSecureFullPath(filePath);

        await SaveFileToPathAsync(fullPath, fileStream, ct);

        _logger.LogInformation("File saved to {FilePath}", fullPath);

        return filePath;
    }

    public async Task<Stream> GetFileAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = GetSecureFullPath(filePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read);
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = GetSecureFullPath(filePath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);

            _logger.LogInformation("File deleted: {FilePath}", fullPath);
        }
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = GetSecureFullPath(filePath);

        return File.Exists(fullPath);
    }

    public async Task<bool> DirectoryExistsAsync(string filePath, CancellationToken ct = default)
    {
        var fullPath = GetSecureFullPath(filePath);

        return Directory.Exists(fullPath);
    }

    public Task<IEnumerable<string>> ListDirectoriesAsync(string path, CancellationToken ct = default)
    {
        var fullPath = GetSecureFullPath(path);

        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var directories = Directory.GetDirectories(fullPath)
            .Select(d => Path.GetRelativePath(_options.BasePath, d))
            .AsEnumerable();

        return Task.FromResult(directories);
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string path, string? pattern = null, CancellationToken ct = default)
    {
        var fullPath = GetSecureFullPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {path}");
        }

        var files = Directory.GetFiles(fullPath)
            .Select(d => Path.GetRelativePath(_options.BasePath, d))
            .AsEnumerable();

        return files;
    }

    public async Task<string> ReadFileAsStringAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = await GetFileAsync(filePath, ct);

        return await new StreamReader(stream).ReadToEndAsync(ct);
    }

    private async Task SaveFileToPathAsync(string fullPath, Stream fileStream, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(fullPath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        using var fileStreamWriter = new FileStream(
            fullPath, FileMode.Create, FileAccess.Write);

        await fileStream.CopyToAsync(fileStreamWriter, ct);
    }

    private void EnsureBaseDirectoryExists()
    {
        if (!Directory.Exists(_options.BasePath))
        {
            Directory.CreateDirectory(_options.BasePath);
        }
    }

    private string GetSecureFullPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(relativePath));
        }

        var cleanPath = relativePath.Replace('/', Path.DirectorySeparatorChar);

        var combinedPath = Path.Combine(
            _options.BasePath,
            cleanPath);

        var fullPath = Path.GetFullPath(combinedPath);

        if (!IsPathWithinBasePath(fullPath))
        {
            throw new UnauthorizedAccessException(
                $"Access denied: path '{fullPath}' resolves outside of allowed base directory");
        }

        return fullPath;
    }

    private bool IsPathWithinBasePath(string fullPath)
    {
        try
        {
            var normalizedBasePath = Path.GetFullPath(_options.BasePath);
            var normalizedFullPath = Path.GetFullPath(fullPath);

            if (!normalizedBasePath.EndsWith(Path.DirectorySeparatorChar))
            {
                normalizedBasePath += Path.DirectorySeparatorChar;
            }

            if (!normalizedFullPath.EndsWith(Path.DirectorySeparatorChar))
            {
                normalizedFullPath += Path.DirectorySeparatorChar;
            }

            return normalizedFullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
