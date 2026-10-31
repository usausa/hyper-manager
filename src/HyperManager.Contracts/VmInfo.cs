namespace HyperManager.Contracts;

// Resource values are only set while the VM is running.
public sealed record VmInfo(
    string Id,
    string Name,
    VmState State,
    int? CpuUsagePercent,
    long? MemoryUsageMb,
    TimeSpan? Uptime,
    string? Version);
