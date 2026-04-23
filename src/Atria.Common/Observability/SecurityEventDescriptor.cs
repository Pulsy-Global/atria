using Microsoft.Extensions.Logging;

namespace Atria.Common.Observability;

public readonly record struct SecurityEventDescriptor(EventId EventId, SecuritySeverity Severity);
