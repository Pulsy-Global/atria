using Microsoft.Extensions.Logging;

namespace Atria.Common.Observability;

public static class SecurityLogExtensions
{
    private const string CategoryKey = "Category";
    private const string CategoryValue = "Security";
    private const string SeverityKey = "SecuritySeverity";

    public static void LogSecurityEvent(
        this ILogger logger,
        SecurityEventDescriptor descriptor,
        Exception? exception,
        string messageTemplate,
        params object?[] args)
    {
        var scope = new Dictionary<string, object>
        {
            [CategoryKey] = CategoryValue,
            [SeverityKey] = descriptor.Severity.ToString(),
        };

        using (logger.BeginScope(scope))
        {
            logger.Log(ToLogLevel(descriptor.Severity), descriptor.EventId, exception, messageTemplate, args);
        }
    }

    private static LogLevel ToLogLevel(SecuritySeverity severity) => severity switch
    {
        SecuritySeverity.Low => LogLevel.Information,
        SecuritySeverity.Medium => LogLevel.Warning,
        SecuritySeverity.High => LogLevel.Error,
        SecuritySeverity.Critical => LogLevel.Critical,
        _ => LogLevel.Warning,
    };
}
