using System.Threading.Channels;

namespace Atria.Feed.Ingestor.ChainClients.Interfaces;

public interface IEvmWebSocketClient
{
    Task ListenAsync(ChannelWriter<bool> signal, TimeSpan inactivityTimeout, CancellationToken ct);
}
