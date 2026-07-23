namespace Atria.Feed.Runtime.Engine.Exceptions;

public sealed class OutputPublishException : Exception
{
    public OutputPublishException(Exception innerException)
        : base("Feed output could not be published.", innerException)
    {
    }
}
