using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Sockets;

namespace Atria.Common.Net;

public class SsrfSafeHandlerFactory
{
    private readonly IOptions<SsrfGuardOptions> _options;

    public SsrfSafeHandlerFactory(IOptions<SsrfGuardOptions> options)
    {
        _options = options;
    }

    public SocketsHttpHandler Create()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            ConnectCallback = ConnectSafeAsync,
        };
    }

    private static async ValueTask<Stream> OpenSocketAsync(
        IPAddress ip,
        int port,
        SsrfGuardOptions opts,
        CancellationToken ct)
    {
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(opts.ConnectTimeoutSeconds));
            await socket.ConnectAsync(ip, port, cts.Token);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private async ValueTask<Stream> ConnectSafeAsync(
        SocketsHttpConnectionContext ctx,
        CancellationToken ct)
    {
        var opts = _options.Value;
        var host = ctx.DnsEndPoint.Host;
        var port = ctx.DnsEndPoint.Port;

        if (IPAddress.TryParse(host, out var literal))
        {
            if (IpAddressRules.IsForbidden(literal, opts))
            {
                throw new SsrfBlockedException(host, new[] { literal });
            }

            return await OpenSocketAsync(literal, port, opts, ct);
        }

        var addresses = await Dns.GetHostAddressesAsync(host, ct);

        if (addresses.Length == 0)
        {
            throw new IOException($"No addresses resolved for host '{host}'");
        }

        var safe = addresses
            .Where(a => !IpAddressRules.IsForbidden(a, opts))
            .ToArray();

        if (safe.Length == 0)
        {
            throw new SsrfBlockedException(host, addresses);
        }

        Exception? last = null;

        foreach (var ip in safe)
        {
            try
            {
                return await OpenSocketAsync(ip, port, opts, ct);
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        throw last ?? new IOException($"No reachable safe address for {host}");
    }
}
