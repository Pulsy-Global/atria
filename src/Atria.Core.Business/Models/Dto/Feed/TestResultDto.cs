namespace Atria.Core.Business.Models.Dto.Feed;

public class TestResultDto
{
    public string? FilterResult { get; set; }

    public string? FunctionResult { get; set; }

    public ExecutionErrorDto? FilterError { get; set; }

    public ExecutionErrorDto? FunctionError { get; set; }
}
