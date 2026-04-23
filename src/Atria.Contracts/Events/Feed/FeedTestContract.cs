using Atria.Contracts.Events.Feed.Enums;

namespace Atria.Contracts.Events.Feed;

public sealed record FeedTestRequest(
    string BlockNumber,
    FeedDeployRequest DeployRequest,
    bool ExecuteOutputs = false);

public sealed record FeedTestResponse(
    object? FilterResult = null,
    object? FunctionResult = null,
    string? ServerError = null,
    FilterErrorData? FilterError = null,
    FilterErrorData? FunctionError = null);

public sealed record FilterErrorData(
    string Message,
    int? Line = 0,
    int? Column = 0);

public sealed record FeedDataRequest(
    FeedDataType DataType,
    string BlockNumber);

public sealed record FeedDataResponse(
    bool Success,
    object? Data,
    string? Error);
