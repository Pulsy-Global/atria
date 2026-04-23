using Atria.Feed.Ingestor.Config.Options;
using Microsoft.Extensions.Options;

namespace Atria.Feed.Ingestor.ChainClients;

public class ReorgHeaderHandler : DelegatingHandler
{
    private readonly Dictionary<string, string> _reorgHeaders;

    public ReorgHeaderHandler(IOptions<IngestorOptions> options)
    {
        _reorgHeaders = new Dictionary<string, string>(options.Value.HttpClient.OnReorgHeaders);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (ReorgContext.IsActive)
        {
            foreach (var (key, value) in _reorgHeaders)
            {
                request.Headers.TryAddWithoutValidation(key, value);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
