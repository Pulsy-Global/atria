using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Models.Crd.Function;
using Atria.Feed.Runtime.Engine.Functions.Clients.Fission.ResourceFactories;
using k8s;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Atria.Feed.Runtime.Engine.Functions.Clients.Fission.Services;

public class FissionKubernetesService
{
    private const string FissionApiGroup = "fission.io";
    private const string FissionApiVersion = "v1";
    private const string PackageResourceType = "packages";
    private const string FunctionResourceType = "functions";
    private const string HttpTriggerResourceType = "httptriggers";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly Kubernetes _kubernetesClient;
    private readonly ILogger<FissionKubernetesService> _logger;

    public FissionKubernetesService(Kubernetes kubernetesClient, ILogger<FissionKubernetesService> logger)
    {
        _kubernetesClient = kubernetesClient;
        _logger = logger;
    }

    public async Task CreatePackageAsync(FissionFunctionDeployment deployment, string functionsNamespace, CancellationToken ct)
    {
        _logger.LogDebug("Creating package resource for function '{FunctionName}'", deployment.Name);

        var packageResource = FissionResourceFactory.CreatePackage(deployment, functionsNamespace);

        await _kubernetesClient.CreateNamespacedCustomObjectAsync(
            packageResource,
            FissionApiGroup,
            FissionApiVersion,
            functionsNamespace,
            PackageResourceType,
            cancellationToken: ct);

        _logger.LogInformation(
            "Created package resource for function '{FunctionName}' in namespace '{Namespace}'",
            deployment.Name,
            functionsNamespace);
    }

    public async Task CreateFunctionAsync(FissionFunctionDeployment deployment, string functionsNamespace, CancellationToken ct)
    {
        _logger.LogDebug("Creating function resource for function '{FunctionName}'", deployment.Name);

        var functionResource = FissionResourceFactory.CreateFunction(deployment, functionsNamespace);

        await _kubernetesClient.CreateNamespacedCustomObjectAsync(
            functionResource,
            FissionApiGroup,
            FissionApiVersion,
            functionsNamespace,
            FunctionResourceType,
            cancellationToken: ct);

        _logger.LogInformation(
            "Created function resource for function '{FunctionName}' in namespace '{Namespace}'",
            deployment.Name,
            functionsNamespace);
    }

    public async Task UpdatePackageAsync(FissionFunctionDeployment deployment, string functionsNamespace, CancellationToken ct)
    {
        _logger.LogDebug("Updating package resource for function '{FunctionName}'", deployment.Name);

        var existingPackage = await _kubernetesClient.GetNamespacedCustomObjectAsync(
            FissionApiGroup,
            FissionApiVersion,
            functionsNamespace,
            PackageResourceType,
            deployment.Name,
            cancellationToken: ct);

        var resourceVersion = GetResourceVersion(existingPackage);
        var packageResource = FissionResourceFactory.CreatePackage(deployment, functionsNamespace, resourceVersion);

        await _kubernetesClient.ReplaceNamespacedCustomObjectAsync(
            packageResource,
            FissionApiGroup,
            FissionApiVersion,
            functionsNamespace,
            PackageResourceType,
            deployment.Name,
            cancellationToken: ct);

        _logger.LogInformation("Updated package resource for function '{FunctionName}'", deployment.Name);
    }

    public async Task CreateHttpTriggerAsync(FissionFunctionDeployment deployment, string functionsNamespace, CancellationToken ct)
    {
        var triggerName = $"{deployment.Name}-trigger";

        _logger.LogInformation(
            "Creating HTTP trigger '{TriggerName}' for function '{FunctionName}'",
            triggerName,
            deployment.Name);

        var triggerResource = FissionResourceFactory.CreateHttpTrigger(deployment, functionsNamespace);

        await _kubernetesClient.CreateNamespacedCustomObjectAsync(
            triggerResource,
            FissionApiGroup,
            FissionApiVersion,
            functionsNamespace,
            HttpTriggerResourceType,
            cancellationToken: ct);

        _logger.LogInformation(
            "Created HTTP trigger '{TriggerName}' for function '{FunctionName}' in namespace '{Namespace}'",
            triggerName,
            deployment.Name,
            functionsNamespace);
    }

    public async Task<bool> IsPackageReadyAsync(string packageName, string functionsNamespace, CancellationToken ct)
    {
        try
        {
            var packageResource = await _kubernetesClient.GetNamespacedCustomObjectAsync(
                FissionApiGroup,
                FissionApiVersion,
                functionsNamespace,
                PackageResourceType,
                packageName,
                cancellationToken: ct);

            var packageJson = JsonSerializer.Serialize(packageResource, JsonOptions);
            var packageDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(packageJson);

            if (packageDict?.ContainsKey("status") != true)
            {
                return false;
            }

            var status = packageDict["status"];
            if (!status.TryGetProperty("buildstatus", out var buildStatus))
            {
                return false;
            }

            return buildStatus.GetString() == "succeeded";
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<bool> IsFunctionReadyAsync(string functionName, string functionsNamespace, CancellationToken ct)
    {
        try
        {
            var functionResource = await _kubernetesClient.GetNamespacedCustomObjectAsync(
                FissionApiGroup,
                FissionApiVersion,
                functionsNamespace,
                FunctionResourceType,
                functionName,
                cancellationToken: ct);

            var functionJson = JsonSerializer.Serialize(functionResource, JsonOptions);
            var functionDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(functionJson);

            if (functionDict?.ContainsKey("spec") != true)
            {
                return false;
            }

            var spec = functionDict["spec"];
            if (!spec.TryGetProperty("package", out var packageRef) ||
                !packageRef.TryGetProperty("packageref", out var packageRefObj) ||
                !packageRefObj.TryGetProperty("name", out var packageName))
            {
                return false;
            }

            var packageNameStr = packageName.GetString();
            if (string.IsNullOrEmpty(packageNameStr))
            {
                return false;
            }

            return await IsPackageReadyAsync(packageNameStr, functionsNamespace, ct);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task DeleteFunctionAsync(string functionId, string functionsNamespace, CancellationToken ct)
    {
        _logger.LogInformation("Deleting function '{FunctionName}'", functionId);

        await _kubernetesClient.DeleteNamespacedCustomObjectAsync(
            FissionApiGroup,
            FissionApiVersion,
            functionsNamespace,
            FunctionResourceType,
            functionId,
            cancellationToken: ct);

        _logger.LogInformation("Successfully deleted function '{FunctionName}'", functionId);
    }

    public async Task DeletePackageAsync(string packageId, string functionsNamespace, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Deleting package '{PackageName}'", packageId);

            await _kubernetesClient.DeleteNamespacedCustomObjectAsync(
                FissionApiGroup,
                FissionApiVersion,
                functionsNamespace,
                PackageResourceType,
                packageId,
                cancellationToken: ct);

            _logger.LogInformation("Successfully deleted package '{PackageName}'", packageId);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Package '{PackageName}' not found during deletion", packageId);
        }
    }

    public async Task DeleteHttpTriggerAsync(string functionId, string functionsNamespace, CancellationToken ct)
    {
        var triggerName = $"{functionId}-trigger";

        try
        {
            _logger.LogInformation("Deleting HTTP trigger '{TriggerName}'", triggerName);

            await _kubernetesClient.DeleteNamespacedCustomObjectAsync(
                FissionApiGroup,
                FissionApiVersion,
                functionsNamespace,
                HttpTriggerResourceType,
                triggerName,
                cancellationToken: ct);

            _logger.LogInformation("Successfully deleted HTTP trigger '{TriggerName}'", triggerName);
        }
        catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("HTTP trigger '{TriggerName}' for function '{FunctionId}' not found during deletion", triggerName, functionId);
        }
    }

    private static string? GetResourceVersion(object resource)
    {
        var resourceJson = JsonSerializer.Serialize(resource, JsonOptions);
        var resourceDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resourceJson);
        return resourceDict?["metadata"].GetProperty("resourceVersion").GetString();
    }
}
