namespace HyperManager.Models;

// Latest status of all configured hosts. UpdatedAt is null until the first refresh.
public sealed record MonitorSnapshot(DateTimeOffset? UpdatedAt, IReadOnlyList<HostStatus> Hosts)
{
    public static MonitorSnapshot Empty { get; } = new(null, []);
}
