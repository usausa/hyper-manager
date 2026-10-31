namespace HyperManager.Host.Application.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;

// Reports the reachability of the agents from the latest snapshot (does not query them)
public sealed class AgentHealthCheck : IHealthCheck
{
    private readonly MonitorService monitorService;

    public AgentHealthCheck(MonitorService monitorService)
    {
        this.monitorService = monitorService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var unreachable = monitorService.Current.Hosts
            .Where(static x => !x.IsReachable)
            .Select(static x => x.Name)
            .ToList();
        return Task.FromResult(unreachable.Count == 0
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Degraded($"Unreachable hosts: {String.Join(", ", unreachable)}"));
    }
}
