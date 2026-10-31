namespace HyperManager;

using System.Net.Http.Json;
using System.Text.Json.Nodes;

using HyperManager.Agent.HyperV;
using HyperManager.Contracts;

public sealed class ApiTests : IClassFixture<AgentApplicationFactory>
{
    private const string VmId = "6F1B0B9E-3A55-4B79-9C63-2B4E5B8A0C11";

    private readonly AgentApplicationFactory factory;

    public ApiTests(AgentApplicationFactory factory)
    {
        this.factory = factory;
        factory.HyperV.Reset();
    }

    //--------------------------------------------------------------------------------
    // Authentication
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task HealthDoesNotRequireApiKey()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ApiWithoutApiKeyReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri(AgentApi.SnapshotPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ApiWithWrongApiKeyReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(AgentApi.ApiKeyHeader, "wrong-key");

        // Act
        var response = await client.GetAsync(new Uri(AgentApi.HostPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    //--------------------------------------------------------------------------------
    // Query
    //--------------------------------------------------------------------------------

    [Fact]
    public async Task HostReturnsMachineName()
    {
        // Arrange
        factory.HyperV.IsElevated = false;
        var client = factory.CreateAuthorizedClient();

        // Act
        var host = await client.GetFromJsonAsync<HostInfo>(new Uri(AgentApi.HostPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(host);
        Assert.Equal(Environment.MachineName, host.Name);
        Assert.False(host.IsElevated);
    }

    [Fact]
    public async Task SnapshotReturnsMetricsAndVms()
    {
        // Arrange
        factory.HyperV.Vms = [new VmInfo(VmId, "vm-1", VmState.Running, 5, 2048, TimeSpan.FromHours(1), "10.0")];
        var client = factory.CreateAuthorizedClient();

        // Act
        var snapshot = await client.GetFromJsonAsync<HostSnapshot>(new Uri(AgentApi.SnapshotPath, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(snapshot);
        Assert.True(snapshot.Host.IsElevated);
        Assert.Equal(12.5, snapshot.Metrics?.CpuUsagePercent);
        var vm = Assert.Single(snapshot.Vms);
        Assert.Equal(VmState.Running, vm.State);
        Assert.Equal(TimeSpan.FromHours(1), vm.Uptime);
    }

    [Fact]
    public async Task SnapshotWithoutMetricsReturnsVms()
    {
        // Arrange
        factory.HyperV.MetricsError = new HyperVException("counter unavailable");
        factory.HyperV.Vms = [new VmInfo(VmId, "vm-1", VmState.Off, null, null, null, "10.0")];
        var client = factory.CreateAuthorizedClient();

        // Act
        var json = JsonNode.Parse(await client.GetStringAsync(new Uri(AgentApi.SnapshotPath, UriKind.Relative), TestContext.Current.CancellationToken))!;

        // Assert
        Assert.Null(json["metrics"]);
        var vm = Assert.Single(json["vms"]!.AsArray());
        Assert.Equal("Off", (string?)vm!["state"]);
    }

    [Fact]
    public async Task VmListFailureReturnsProblem()
    {
        // Arrange
        factory.HyperV.QueryError = new HyperVException("query failed");
        var client = factory.CreateAuthorizedClient();

        // Act
        var response = await client.GetAsync(new Uri(AgentApi.VmsPath, UriKind.Relative), TestContext.Current.CancellationToken);
        var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("query failed", (string?)problem["detail"]);
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    [Theory]
    [InlineData(VmOperation.Start, nameof(IHyperVService.StartAsync))]
    [InlineData(VmOperation.Resume, nameof(IHyperVService.ResumeAsync))]
    [InlineData(VmOperation.Pause, nameof(IHyperVService.PauseAsync))]
    [InlineData(VmOperation.Save, nameof(IHyperVService.SaveAsync))]
    [InlineData(VmOperation.Shutdown, nameof(IHyperVService.ShutdownAsync))]
    [InlineData(VmOperation.TurnOff, nameof(IHyperVService.TurnOffAsync))]
    public async Task OperationPathOfClientIsRouted(VmOperation operation, string expected)
    {
        // Arrange
        var client = factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsync(new Uri(AgentApi.OperationPath(VmId, operation), UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var call = Assert.Single(factory.HyperV.Calls);
        Assert.Equal(expected, call.Name);
        Assert.Equal(VmId, call.Id);
    }

    [Fact]
    public async Task ShutdownPassesForce()
    {
        // Arrange
        var client = factory.CreateAuthorizedClient();
        using var content = JsonContent.Create(new ShutdownRequest(true));

        // Act
        var response = await client.PostAsync(new Uri(AgentApi.OperationPath(VmId, VmOperation.Shutdown), UriKind.Relative), content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(Assert.Single(factory.HyperV.Calls).Force);
    }

    [Fact]
    public async Task OperationOfUnknownVmReturnsNotFound()
    {
        // Arrange
        factory.HyperV.OperationError = new VmNotFoundException("not found");
        var client = factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsync(new Uri(AgentApi.OperationPath(VmId, VmOperation.Start), UriKind.Relative), null, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task OperationFailureReturnsConflict()
    {
        // Arrange
        factory.HyperV.OperationError = new HyperVException("invalid state");
        var client = factory.CreateAuthorizedClient();

        // Act
        var response = await client.PostAsync(new Uri(AgentApi.OperationPath(VmId, VmOperation.Pause), UriKind.Relative), null, TestContext.Current.CancellationToken);
        var problem = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("invalid state", (string?)problem["detail"]);
    }
}
