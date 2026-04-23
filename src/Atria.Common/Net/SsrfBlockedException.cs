using Atria.Common.Exceptions;
using System.Net;

namespace Atria.Common.Net;

public class SsrfBlockedException : BaseException
{
    public string Host { get; }

    public IReadOnlyList<IPAddress> ResolvedAddresses { get; }

    public SsrfBlockedException(string host, IEnumerable<IPAddress> addresses)
        : base($"Host '{host}' resolves to forbidden address(es): {string.Join(", ", addresses)}")
    {
        Host = host;
        ResolvedAddresses = addresses.ToList();
    }
}
