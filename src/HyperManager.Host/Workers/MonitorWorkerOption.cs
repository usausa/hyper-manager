namespace HyperManager.Host.Workers;

public sealed class MonitorWorkerOption
{
    // false: no automatic refresh (manual refresh only)
    public bool Enable { get; set; }

    [Range(1, 3600)]
    public int IntervalSeconds { get; set; } = 10;
}
