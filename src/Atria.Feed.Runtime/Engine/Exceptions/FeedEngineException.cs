using Atria.Feed.Runtime.Engine.Filters.Js.Extensions;
using Atria.Feed.Runtime.Engine.Functions.Helpers;
using Microsoft.ClearScript;

namespace Atria.Feed.Runtime.Engine.Exceptions;

public class FeedEngineException : Exception
{
    public string? SourceCode { get; }
    public int Line { get; }
    public int Column { get; }
    public override string Message { get; }

    public bool IsFunctionError { get; } = false;

    public FeedEngineException(string message, string sourceCode, ScriptEngineException innerException)
        : base(message, innerException)
    {
        var position = innerException.GetErrorPosition();

        Message = message;
        SourceCode = sourceCode;
        Line = position.line;
        Column = position.column;
    }

    public FeedEngineException(string message, string? sourceCode = null, bool isFunctionError = false)
        : base(message)
    {
        var (line, column) = FeedEngineHelper.ParseNodeJsErrorPosition(message);

        Message = message;
        SourceCode = sourceCode;
        Line = line;
        Column = column;
        IsFunctionError = isFunctionError;
    }
}
