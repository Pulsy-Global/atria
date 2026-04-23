using Atria.Orchestrator.Models.Business;

namespace Atria.Orchestrator.Services.Interfaces;

public interface IManifestScanner
{
    bool HasChanges(ScanResult<FeedDeployment> scanResult);

    Task<List<DirectoryScanResult<TResult>>> ScanDirectoryAsync<TResult>(
        string directoryPath,
        string fileName,
        Func<TResult, bool>? validate = null)
        where TResult : class;
}
