namespace HyperManager;

using System.Collections.Concurrent;

using HyperManager.Contracts;
using HyperManager.Infrastructure.Agents;

public sealed record ExecutedOperation(string HostName, string VmId, VmOperation Operation);

public sealed class StubAgentClient : IAgentClient
{
    private readonly ConcurrentQueue<ExecutedOperation> executed = new();

    // Failure returned by the agent, keyed by VM id
    public ConcurrentDictionary<string, AgentException> Failures { get; } = new(StringComparer.Ordinal);

    public IReadOnlyCollection<ExecutedOperation> Executed => executed;

    public ValueTask<HostSnapshot> GetSnapshotAsync(AgentHostOption host, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new HostSnapshot(new HostInfo(host.Name, true), null, []));

    public ValueTask ExecuteAsync(AgentHostOption host, string vmId, VmOperation operation, CancellationToken cancellationToken = default)
    {
        if (Failures.TryGetValue(vmId, out var failure))
        {
            return ValueTask.FromException(failure);
        }

        executed.Enqueue(new ExecutedOperation(host.Name, vmId, operation));
        return ValueTask.CompletedTask;
    }
}
