namespace Atria.Common.Exceptions;

public class ConcurrencyException : InvalidOperationException
{
    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
