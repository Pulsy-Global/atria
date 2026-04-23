namespace Atria.Business.Services.Storage.Interfaces;

public interface IFileSystemService
{
    Task<string> SaveFileAsync(string filePath, Stream fileStream, CancellationToken ct = default);

    Task<Stream> GetFileAsync(string filePath, CancellationToken ct = default);

    Task DeleteFileAsync(string filePath, CancellationToken ct = default);

    Task<bool> FileExistsAsync(string filePath, CancellationToken ct = default);

    Task<bool> DirectoryExistsAsync(string filePath, CancellationToken ct = default);

    Task<IEnumerable<string>> ListDirectoriesAsync(string path, CancellationToken ct = default);

    Task<IEnumerable<string>> ListFilesAsync(string path, string? pattern = null, CancellationToken ct = default);

    Task<string> ReadFileAsStringAsync(string filePath, CancellationToken ct = default);
}
