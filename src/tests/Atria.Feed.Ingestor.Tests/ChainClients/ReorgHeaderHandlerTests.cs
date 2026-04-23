using Atria.Feed.Ingestor.ChainClients;
using Atria.Feed.Ingestor.Config.Options;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Net;

namespace Atria.Feed.Ingestor.Tests.ChainClients;

public class ReorgHeaderHandlerTests
{
    [Fact]
    public async Task SendAsync_ReorgInactive_NoHeadersAdded()
    {
        var headers = new Dictionary<string, string> { { "X-ERPC-Skip-Cache-Read", "true" } };
        var (client, inner) = CreateTestClient(headers);

        await client.GetAsync("http://localhost/test");

        inner.CapturedRequest!.Headers.Contains("X-ERPC-Skip-Cache-Read").Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ReorgActive_HeadersAdded()
    {
        var headers = new Dictionary<string, string>
        {
            { "X-ERPC-Skip-Cache-Read", "true" },
            { "X-Custom-Reorg", "1" },
        };
        var (client, inner) = CreateTestClient(headers);

        using var scope = ReorgContext.Activate();
        await client.GetAsync("http://localhost/test");

        inner.CapturedRequest!.Headers.GetValues("X-ERPC-Skip-Cache-Read").Should().ContainSingle().Which.Should().Be("true");
        inner.CapturedRequest.Headers.GetValues("X-Custom-Reorg").Should().ContainSingle().Which.Should().Be("1");
    }

    [Fact]
    public async Task SendAsync_ReorgActive_EmptyHeaders_NoCustomHeadersAdded()
    {
        var (client, inner) = CreateTestClient(new Dictionary<string, string>());

        using var scope = ReorgContext.Activate();
        await client.GetAsync("http://localhost/test");

        inner.CapturedRequest!.Headers.Contains("X-ERPC-Skip-Cache-Read").Should().BeFalse();
    }

    private static (HttpClient Client, MockHandler Inner) CreateTestClient(Dictionary<string, string> reorgHeaders)
    {
        var options = Options.Create(new IngestorOptions
        {
            HttpClient = new IngestorHttpClientOptions { OnReorgHeaders = reorgHeaders },
        });
        var handler = new ReorgHeaderHandler(options) { InnerHandler = new MockHandler() };
        var client = new HttpClient(handler);
        return (client, (MockHandler)handler.InnerHandler);
    }

    private class MockHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
