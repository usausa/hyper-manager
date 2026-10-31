namespace HyperManager.Services;

using HyperManager.Infrastructure.Agents;

// Holds the latest status of all hosts and notifies subscribers when it is refreshed.
// The refresh is shared by every page, so the load of the agents does not depend on the number of browsers.
public sealed class MonitorService : IDisposable
{
    private readonly ILogger<MonitorService> log;

    private readonly TimeProvider timeProvider;

    private readonly IAgentClient agentClient;

    private readonly AgentClientOption option;

    private readonly SemaphoreSlim refreshLock = new(1, 1);

    private MonitorSnapshot current = MonitorSnapshot.Empty;

    public event EventHandler<MonitorEventArgs>? Updated;

    // The most recently refreshed snapshot. Used for immediate display when a page is opened.
    public MonitorSnapshot Current => Volatile.Read(ref current);

    public MonitorService(
        ILogger<MonitorService> log,
        TimeProvider timeProvider,
        IAgentClient agentClient,
        AgentClientOption option)
    {
        this.log = log;
        this.timeProvider = timeProvider;
        this.agentClient = agentClient;
        this.option = option;
    }

    public void Dispose()
    {
        refreshLock.Dispose();
    }

    // Queries every host in parallel. Per-host failures are reported by HostStatus.Error.
    public async ValueTask<MonitorSnapshot> RefreshAsync(CancellationToken cancellationToken = default)
    {
        // Serialize refreshes so that an older result never overwrites a newer one
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            var hosts = await Task.WhenAll(option.Hosts.Select(x => QueryHostAsync(x, cancellationToken)));
            var snapshot = new MonitorSnapshot(timeProvider.GetLocalNow(), hosts);

            Volatile.Write(ref current, snapshot);
            Updated?.Invoke(this, new MonitorEventArgs(snapshot));

            return snapshot;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private async Task<HostStatus> QueryHostAsync(AgentHostOption host, CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await agentClient.GetSnapshotAsync(host, cancellationToken);
            return new HostStatus(host.Name, snapshot.Host, snapshot.Metrics, snapshot.Vms, null);
        }
        catch (AgentException ex)
        {
            log.WarnHostQueryFailed(host.Name, host.Url, ex.Message);
            return new HostStatus(host.Name, null, null, [], ex.Message);
        }
    }
}
