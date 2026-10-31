namespace HyperManager.Infrastructure.Agents;

// Client of the HyperManager.Agent API. Failures are reported by AgentException.
public interface IAgentClient
{
    ValueTask<HostSnapshot> GetSnapshotAsync(AgentHostOption host, CancellationToken cancellationToken = default);

    ValueTask ExecuteAsync(AgentHostOption host, string vmId, VmOperation operation, CancellationToken cancellationToken = default);
}
