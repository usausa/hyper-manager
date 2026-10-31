namespace HyperManager;

using System.Collections.Concurrent;

using HyperManager.Agent.HyperV;
using HyperManager.Contracts;

public sealed record HyperVCall(string Name, string Id, bool? Force);

public sealed class FakeHyperVService : IHyperVService
{
    private readonly ConcurrentQueue<HyperVCall> calls = new();

    public bool IsElevated { get; set; }

    public HostMetrics Metrics { get; set; } = default!;

    public IReadOnlyList<VmInfo> Vms { get; set; } = [];

    public HyperVException? MetricsError { get; set; }

    public HyperVException? QueryError { get; set; }

    public HyperVException? OperationError { get; set; }

    public IReadOnlyCollection<HyperVCall> Calls => calls;

    public FakeHyperVService()
    {
        Reset();
    }

    public void Reset()
    {
        IsElevated = true;
        Metrics = new HostMetrics(12.5, 16384, 8192, [new DiskInfo("C:", 1000, 400)]);
        Vms = [];
        MetricsError = null;
        QueryError = null;
        OperationError = null;
        calls.Clear();
    }

    public ValueTask CheckAsync(CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;

    public ValueTask<IReadOnlyList<VmInfo>> GetVirtualMachinesAsync(CancellationToken cancellationToken = default) =>
        QueryError is null ? ValueTask.FromResult(Vms) : ValueTask.FromException<IReadOnlyList<VmInfo>>(QueryError);

    public ValueTask<HostMetrics> GetHostMetricsAsync(CancellationToken cancellationToken = default) =>
        MetricsError is null ? ValueTask.FromResult(Metrics) : ValueTask.FromException<HostMetrics>(MetricsError);

    public ValueTask StartAsync(string id, CancellationToken cancellationToken = default) =>
        Record(nameof(StartAsync), id, null);

    public ValueTask ResumeAsync(string id, CancellationToken cancellationToken = default) =>
        Record(nameof(ResumeAsync), id, null);

    public ValueTask PauseAsync(string id, CancellationToken cancellationToken = default) =>
        Record(nameof(PauseAsync), id, null);

    public ValueTask SaveAsync(string id, CancellationToken cancellationToken = default) =>
        Record(nameof(SaveAsync), id, null);

    public ValueTask ShutdownAsync(string id, bool force, CancellationToken cancellationToken = default) =>
        Record(nameof(ShutdownAsync), id, force);

    public ValueTask TurnOffAsync(string id, CancellationToken cancellationToken = default) =>
        Record(nameof(TurnOffAsync), id, null);

    private ValueTask Record(string name, string id, bool? force)
    {
        if (OperationError is not null)
        {
            return ValueTask.FromException(OperationError);
        }

        calls.Enqueue(new HyperVCall(name, id, force));
        return ValueTask.CompletedTask;
    }
}
