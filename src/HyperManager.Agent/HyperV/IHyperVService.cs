namespace HyperManager.Agent.HyperV;

public interface IHyperVService
{
    bool IsElevated { get; }

    // Throws HyperVException when the Hyper-V management service is not accessible
    ValueTask CheckAsync(CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<VmInfo>> GetVirtualMachinesAsync(CancellationToken cancellationToken = default);

    ValueTask<HostMetrics> GetHostMetricsAsync(CancellationToken cancellationToken = default);

    ValueTask StartAsync(string id, CancellationToken cancellationToken = default);

    ValueTask ResumeAsync(string id, CancellationToken cancellationToken = default);

    ValueTask PauseAsync(string id, CancellationToken cancellationToken = default);

    ValueTask SaveAsync(string id, CancellationToken cancellationToken = default);

    ValueTask ShutdownAsync(string id, bool force, CancellationToken cancellationToken = default);

    ValueTask TurnOffAsync(string id, CancellationToken cancellationToken = default);
}
