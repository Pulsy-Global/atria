using Microsoft.Extensions.Logging;

namespace Atria.Common.Observability;

// EventId range reserved for OSS security events: 1000-1999.
public static class SecurityEvents
{
    public static readonly SecurityEventDescriptor SsrfBlocked = new(
        new EventId(1000, nameof(SsrfBlocked)),
        SecuritySeverity.Medium);
}
