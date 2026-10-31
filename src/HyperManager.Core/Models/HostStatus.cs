namespace HyperManager.Models;

// Result of querying the agent of a single host.
// Error is non-null when the host was unreachable or the request failed; in that case Vms is empty.
public sealed record HostStatus(
    string Name,
    HostInfo? Info,
    HostMetrics? Metrics,
    IReadOnlyList<VmInfo> Vms,
    string? Error)
{
    public bool IsReachable => Error is null;
}
