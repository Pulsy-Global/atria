using Atria.Contracts.Events.Feed.Enums;

namespace Atria.Contracts.Events.Feed;

public record FeedDeployRequest(
    string Id,
    string ChainId,
    string? FilterCode,
    string? FunctionCode,
    List<string>? OutputIds,
    FeedDataType FeedDataType,
    FeedType Type,
    FilterLangKind FilterLangKind = FilterLangKind.JavaScript,
    FunctionLangKind FunctionLangKind = FunctionLangKind.JavaScript,
    int BlockDelay = 0,
    ulong? StartBlock = null,
    ErrorHandlingStrategy ErrorHandling = ErrorHandlingStrategy.StopOnError,
    string? EkvNamespace = null);

public sealed record FeedPauseRequest(
    string Id,
    FeedPauseSource Source = FeedPauseSource.User,
    string? Reason = null);

public sealed record FeedDeleteRequest(string Id);

public sealed record FeedPausedEvent(
    string FeedId,
    FeedPauseSource Source);

public sealed record FeedDeployedEvent(string FeedId);
