using System.Net;
using System.Net.Sockets;

namespace Atria.Common.Net;

public static class IpAddressRules
{
    public static bool IsForbidden(IPAddress ip, SsrfGuardOptions options)
    {
        if (!options.Enabled)
        {
            return false;
        }

        var normalized = ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip;

        if (IPAddress.IsLoopback(normalized))
        {
            return true;
        }

        if (normalized.Equals(IPAddress.Any) || normalized.Equals(IPAddress.IPv6Any))
        {
            return true;
        }

        if (normalized.AddressFamily == AddressFamily.InterNetwork)
        {
            return IsForbiddenIPv4(normalized.GetAddressBytes());
        }

        if (normalized.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return IsForbiddenIPv6(normalized);
        }

        return true;
    }

    private static bool IsForbiddenIPv4(byte[] bytes)
    {
        // 10.0.0.0/8
        if (bytes[0] == 10)
        {
            return true;
        }

        // 172.16.0.0/12
        if (bytes[0] == 172 && (bytes[1] & 0xF0) == 16)
        {
            return true;
        }

        // 192.168.0.0/16
        if (bytes[0] == 192 && bytes[1] == 168)
        {
            return true;
        }

        // 169.254.0.0/16 (link-local / cloud metadata)
        if (bytes[0] == 169 && bytes[1] == 254)
        {
            return true;
        }

        // 100.64.0.0/10 (CGNAT)
        if (bytes[0] == 100 && (bytes[1] & 0xC0) == 64)
        {
            return true;
        }

        // 0.0.0.0/8 — routes to localhost on Linux
        if (bytes[0] == 0)
        {
            return true;
        }

        // 224.0.0.0/4 multicast, 240.0.0.0/4 reserved
        if (bytes[0] >= 224)
        {
            return true;
        }

        return false;
    }

    private static bool IsForbiddenIPv6(IPAddress ip)
    {
        if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
        {
            return true;
        }

        // fc00::/7 unique-local
        var bytes = ip.GetAddressBytes();
        if ((bytes[0] & 0xFE) == 0xFC)
        {
            return true;
        }

        return false;
    }
}
