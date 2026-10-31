namespace HyperManager.Services;

public sealed class MonitorEventArgs : EventArgs
{
    public MonitorSnapshot Snapshot { get; }

    public MonitorEventArgs(MonitorSnapshot snapshot)
    {
        Snapshot = snapshot;
    }
}
