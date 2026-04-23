using Atria.Contracts.Events.Feed.Enums;
using System.Numerics;

namespace Atria.Feed.Runtime.Engine.Models;

public class FeedRuntime
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public FilterLangKind FilterLangKind { get; set; }
    public FunctionLangKind FunctionLangKind { get; set; }
    public FeedDataType DataType { get; set; }
    public string? FilterCode { get; set; }
    public string? FunctionCode { get; set; }
    public FeedType Type { get; set; }
    public List<string>? OutputIds { get; set; }
    public int BlockDelay { get; set; }
    public BigInteger? StartBlock { get; set; }
    public ErrorHandlingStrategy ErrorHandling { get; set; }
    public string? EkvNamespace { get; set; }
}
