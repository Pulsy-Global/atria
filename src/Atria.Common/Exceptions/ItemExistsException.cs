namespace Atria.Common.Exceptions;

public class ItemExistsException : BaseException
{
    public ItemExistsException()
    {
    }

    public ItemExistsException(string message)
        : base(message)
    {
    }

    public ItemExistsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
