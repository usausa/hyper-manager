namespace HyperManager;

using HyperManager.Contracts;
using HyperManager.Infrastructure.Agents;

// Agent with a stopped VM. Operations always succeed.
public sealed class StubAgentClient : IAgentClient
{
    public const string MachineName = "E2E-HOST";

    public const string VmName = "e2e-vm";

    private const long Gigabyte = 1024L * 1024 * 1024;

    public ValueTask<HostSnapshot> GetSnapshotAsync(AgentHostOption host, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new HostSnapshot(
            new HostInfo(MachineName, true),
            new HostMetrics(25, 16384, 8192, [new DiskInfo("C:", 100 * Gigabyte, 40 * Gigabyte)]),
            [new VmInfo("6F1B0B9E-3A55-4B79-9C63-2B4E5B8A0C11", VmName, VmState.Off, null, null, null, "10.0")]));

    public ValueTask ExecuteAsync(AgentHostOption host, string vmId, VmOperation operation, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
