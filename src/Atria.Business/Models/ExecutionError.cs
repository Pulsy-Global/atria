namespace Atria.Business.Models;

public class ExecutionError
{
    public string Message { get; set; }

    public int? Line { get; set; }

    public int? Column { get; set; }
}
