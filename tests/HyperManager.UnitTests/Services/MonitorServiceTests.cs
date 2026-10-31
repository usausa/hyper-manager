namespace HyperManager.Services;

using HyperManager.Contracts;
using HyperManager.Infrastructure.Agents;
using HyperManager.Models;

using Microsoft.Extensions.Logging.Abstractions;

public sealed class MonitorServiceTests
{
    private static readonly AgentHostOption HostA = new() { Name = "A", Url = new Uri("http://host-a:5100"), ApiKey = "key-a" };

    private static readonly AgentHostOption HostB = new() { Name = "B", Url = new Uri("http://host-b:5100"), ApiKey = "key-b" };

    [Fact]
    public void CurrentIsEmptyBeforeRefresh()
    {
        // Arrange
        using var service = new MonitorService(NullLogger<MonitorService>.Instance, TimeProvider.System, new StubAgentClient(), new AgentClientOption());

        // Assert
        Assert.Null(service.Current.UpdatedAt);
        Assert.Empty(service.Current.Hosts);
    }

    [Fact]
    public async Task RefreshAggregatesHostsAndKeepsFailures()
    {
        // Arrange
        var vm = new VmInfo("1", "vm-1", VmState.Running, 10, 1024, TimeSpan.FromMinutes(1), "10.0");
        var client = new StubAgentClient();
        client.Snapshots[HostA.Name] = () => new HostSnapshot(new HostInfo("PC-A", true), null, [vm]);
        client.Snapshots[HostB.Name] = static () => throw new AgentException("unreachable");

        using var service = new MonitorService(NullLogger<MonitorService>.Instance, TimeProvider.System, client, new AgentClientOption { Hosts = [HostA, HostB] });
        MonitorSnapshot? published = null;
        service.Updated += (_, e) => published = e.Snapshot;

        // Act
        var snapshot = await service.RefreshAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Same(snapshot, service.Current);
        Assert.Same(snapshot, published);
        Assert.NotNull(snapshot.UpdatedAt);
        Assert.Collection(
            snapshot.Hosts,
            static a =>
            {
                Assert.Equal("A", a.Name);
                Assert.True(a.IsReachable);
                Assert.Equal("PC-A", a.Info?.Name);
                Assert.Equal("vm-1", Assert.Single(a.Vms).Name);
            },
            static b =>
            {
                Assert.Equal("B", b.Name);
                Assert.False(b.IsReachable);
                Assert.Equal("unreachable", b.Error);
                Assert.Empty(b.Vms);
            });
    }

    private sealed class StubAgentClient : IAgentClient
    {
        public Dictionary<string, Func<HostSnapshot>> Snapshots { get; } = new(StringComparer.Ordinal);

        public ValueTask<HostSnapshot> GetSnapshotAsync(AgentHostOption host, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Snapshots[host.Name]());

        public ValueTask ExecuteAsync(AgentHostOption host, string vmId, VmOperation operation, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
