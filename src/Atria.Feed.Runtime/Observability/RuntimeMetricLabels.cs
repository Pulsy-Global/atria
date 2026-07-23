namespace Atria.Feed.Runtime.Observability;

public static class RuntimeMetricLabels
{
    public const string Success = "success";
    public const string Failure = "failure";
    public const string Timeout = "timeout";
    public const string FilterError = "filter_error";
    public const string FunctionError = "function_error";
    public const string PublishError = "publish_error";
    public const string MissingData = "missing_data";
    public const string Unknown = "unknown";
}
