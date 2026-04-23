using Microsoft.ClearScript;
using System.Text.RegularExpressions;

namespace Atria.Feed.Runtime.Engine.Filters.Js.Extensions;

public static class ScriptEngineExceptionExtensions
{
    public static (int line, int column) GetErrorPosition(this ScriptEngineException exception)
    {
        if (string.IsNullOrEmpty(exception.ErrorDetails))
        {
            return (0, 0);
        }

        var match = Regex.Match(
            exception.ErrorDetails,
            @"at Script:(\d+):(\d+)");

        if (match.Success)
        {
            return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }

        return (0, 0);
    }
}
