namespace Atria.Business.Models;

public class TestResult
{
    public object? FilterResult { get; set; }

    public object? FunctionResult { get; set; }

    public string? ServerError { get; set; }

    public ExecutionError? FilterError { get; set; }

    public ExecutionError? FunctionError { get; set; }
}
