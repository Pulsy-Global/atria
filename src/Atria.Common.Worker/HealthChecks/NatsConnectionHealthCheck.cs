using Atria.Common.Messaging.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Atria.Common.Worker.HealthChecks;

public sealed class NatsConnectionHealthCheck : IHealthCheck
{
    private readonly NatsConnectionManager _connectionManager;

    public NatsConnectionHealthCheck(NatsConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rtt = await _connectionManager.Connection.PingAsync(cancellationToken);

            return HealthCheckResult.Healthy($"NATS ping successful in {rtt.TotalMilliseconds:F0}ms");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"NATS ping failed. ConnectionState={_connectionManager.Connection.ConnectionState}",
                ex);
        }
    }
}
