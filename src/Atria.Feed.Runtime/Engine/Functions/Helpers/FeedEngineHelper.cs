using System.Text.RegularExpressions;

namespace Atria.Feed.Runtime.Engine.Functions.Helpers;

public static class FeedEngineHelper
{
    public static (int line, int column) ParseNodeJsErrorPosition(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return (0, 0);
        }

        var match = Regex.Match(
            message,
            @"at \w+ \([^:]+:(\d+):(\d+)\)");

        if (match.Success)
        {
            return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
        }

        return (0, 0);
    }
}
