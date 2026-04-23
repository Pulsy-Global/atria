namespace Atria.Core.Business.Models.Dto.Feed;

public class ExecutionErrorDto
{
    public string Message { get; set; }

    public int? Line { get; set; }

    public int? Column { get; set; }
}
