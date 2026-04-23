namespace Atria.Feed.Ingestor.ChainClients.Interfaces;

public interface IEvmRetryService
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName);
}
