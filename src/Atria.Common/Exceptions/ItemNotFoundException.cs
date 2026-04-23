namespace Atria.Common.Exceptions;

public class ItemNotFoundException : BaseException
{
    public ItemNotFoundException(string message)
        : base(message)
    {
    }
}
