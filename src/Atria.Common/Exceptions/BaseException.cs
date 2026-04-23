namespace Atria.Common.Exceptions;

public class BaseException : Exception
{
    public string? ErrorCode { get; init; }

    public BaseException()
    {
    }

    public BaseException(string message)
        : base(message)
    {
    }

    public BaseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
