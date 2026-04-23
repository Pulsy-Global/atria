namespace Atria.Feed.Ingestor.ChainClients.Interfaces;

public interface IClientFactory<T>
    where T : class
{
    T CreateClient();
}
