using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AggregatorPlatform.API.HealthChecks;

public class ExternalApiHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Stub: in production, ping configured partners' /health endpoints.
        return Task.FromResult(HealthCheckResult.Healthy("External APIs reachable (stub)."));
    }
}
