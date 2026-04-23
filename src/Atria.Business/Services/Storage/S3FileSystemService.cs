using Amazon.S3;
using Amazon.S3.Model;
using Atria.Business.Models.Options;
using Atria.Business.Services.Storage.Interfaces;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Atria.Business.Services.Storage;

public class S3FileSystemService : IFileSystemService
{
    private readonly S3StorageOptions _options;
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3FileSystemService> _logger;

    public S3FileSystemService(
        IOptions<S3StorageOptions> options,
        IAmazonS3 s3Client,
        ILogger<S3FileSystemService> logger)
    {
        _options = options.Value;
        _s3Client = s3Client;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(string filePath, Stream fileStream, CancellationToken ct = default)
    {
        var putObjectRequest = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = filePath,
            InputStream = fileStream,
            ContentType = GetContentType(filePath),
            DisablePayloadSigning = true,
        };

        var response = await _s3Client.PutObjectAsync(putObjectRequest, ct);

        if (response.HttpStatusCode == HttpStatusCode.OK)
        {
            return filePath;
        }

        throw new Exception($"Failed to upload file. Status: {response.HttpStatusCode}");
    }

    public async Task<Stream> GetFileAsync(string filePath, CancellationToken ct = default)
    {
        var getObjectRequest = new GetObjectRequest
        {
            BucketName = _options.BucketName,
            Key = filePath,
        };

        var response = await _s3Client.GetObjectAsync(getObjectRequest, ct);

        return response.ResponseStream;
    }

    public async Task DeleteFileAsync(string filePath, CancellationToken ct = default)
    {
        var deleteObjectRequest = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = filePath,
        };

        var response = await _s3Client.DeleteObjectAsync(deleteObjectRequest, ct);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            throw new Exception("Failed to delete file");
        }
    }

    public async Task<bool> FileExistsAsync(string filePath, CancellationToken ct = default)
    {
        var listRequest = new ListObjectsV2Request
        {
            BucketName = _options.BucketName,
            Prefix = filePath,
            MaxKeys = 1,
        };

        var response = await _s3Client.ListObjectsV2Async(listRequest, cancellationToken: ct);

        return response.S3Objects?.Any(obj => obj.Key == filePath) ?? false;
    }

    public async Task<bool> DirectoryExistsAsync(string directoryPath, CancellationToken ct = default)
    {
        var prefix = directoryPath.EndsWith("/") ? directoryPath : directoryPath + "/";

        var listRequest = new ListObjectsV2Request
        {
            BucketName = _options.BucketName,
            Prefix = prefix,
            Delimiter = "/",
        };

        var response = await _s3Client.ListObjectsV2Async(listRequest, cancellationToken: ct);

        return response.CommonPrefixes.Any(p => p == prefix);
    }

    public async Task<IEnumerable<string>> ListDirectoriesAsync(string path, CancellationToken ct = default)
    {
        var prefix = path.EndsWith("/") ? path : path + "/";

        var listRequest = new ListObjectsV2Request
        {
            BucketName = _options.BucketName,
            Prefix = prefix,
            Delimiter = "/",
        };

        var response = await _s3Client.ListObjectsV2Async(listRequest, ct);

        var directories = response.CommonPrefixes
            .Where(prefix => !string.IsNullOrEmpty(prefix))
            .Select(prefix => prefix.TrimEnd('/'))
            .ToList();

        return directories;
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string path, string? pattern = null, CancellationToken ct = default)
    {
        var prefix = path.EndsWith("/") ? path : path + "/";

        var response = await _s3Client.ListObjectsV2Async(
            new ListObjectsV2Request
            {
                BucketName = _options.BucketName,
                Prefix = prefix,
            },
            cancellationToken: ct);

        var files = response.S3Objects
            .Where(obj => obj.Key != null && obj.Key != prefix) // Исключаем сам prefix
            .Select(obj => obj.Key)
            .AsEnumerable();

        if (!string.IsNullOrEmpty(pattern))
        {
            files = files.Where(file => System.Text.RegularExpressions.Regex.IsMatch(file, pattern));
        }

        return files;
    }

    public async Task<string> ReadFileAsStringAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = await GetFileAsync(filePath, ct);

        return await new StreamReader(stream).ReadToEndAsync(ct);
    }

    private static string GetContentType(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();

        if (!provider.TryGetContentType(filePath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return contentType;
    }
}
