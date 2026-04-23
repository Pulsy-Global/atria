namespace Atria.Common.Exceptions;

public class ValidationException : BaseException
{
    private const string DefaultMessage = "Validation error";

    public IDictionary<string, string[]> Errors { get; set; }

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(DefaultMessage)
    {
        Errors = errors;
    }
}
