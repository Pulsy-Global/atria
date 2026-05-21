namespace Atria.Core.Data.Entities.Enums;

public enum DeployErrorCode
{
    DeploymentFailed = 100,
    RuntimeUnavailable = 200,
    WebhookUnavailable = 300,
    ProcessingFailed = 400,
    BlockDataUnavailable = 500,
    OperationFailed = 900,
}
