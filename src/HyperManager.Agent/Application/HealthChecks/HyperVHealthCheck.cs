namespace HyperManager.Agent.Application.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

public sealed class HyperVHealthCheck : IHealthCheck
{
    private readonly IHyperVService hyperV;

    public HyperVHealthCheck(IHyperVService hyperV)
    {
        this.hyperV = hyperV;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await hyperV.CheckAsync(cancellationToken);

            return HealthCheckResult.Healthy();
        }
        catch (HyperVException e)
        {
            return HealthCheckResult.Unhealthy("Hyper-V is not available.", e);
        }
    }
}
