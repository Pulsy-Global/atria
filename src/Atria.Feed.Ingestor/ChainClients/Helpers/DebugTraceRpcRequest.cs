using Atria.Feed.Ingestor.ChainClients.Models;
using Nethereum.Hex.HexTypes;
using System.Net.Http.Json;
using System.Numerics;

namespace Atria.Feed.Ingestor.ChainClients.Helpers;

public class DebugTraceRpcRequest
{
    private const string DebugTraceBlockByNumber = "debug_traceBlockByNumber";

    public static RpcRequest BuildDebugTraceRpcRequest(BigInteger index)
    {
        return new RpcRequest(DebugTraceBlockByNumber, index.ToHexBigInteger(), new Dictionary<string, string>()
        {
            {
                "tracer", "callTracer"
            },
        });
    }

    public static HttpRequestMessage BuildDebugTraceHttpRequestMessage(string uri, RpcRequest rpcRequest)
    {
        return new HttpRequestMessage(HttpMethod.Post, new Uri(uri))
        {
            Content = JsonContent.Create(rpcRequest),
        };
    }
}
