namespace HyperManager;

using System.Net.Http.Json;
using System.Text.Json.Nodes;

using HyperManager.Contracts;
using HyperManager.Host.Application.Context;
using HyperManager.Infrastructure.Agents;
using HyperManager.Services;

using Microsoft.Extensions.DependencyInjection;

public sealed class OperationTests : IClassFixture<TestApplicationFactory>
{
    private const string HostName = "Local";

    private const string UserId = "tester";

    private readonly TestApplicationFactory factory;

    public OperationTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task ExecutedOperationIsRecorded()
    {
        // Arrange
        var vm = CreateVm();

        // Act
        var result = await ExecuteAsync(HostName, vm, VmOperation.Start);
        var item = Assert.Single(await QueryHistoryAsync(vm.Name));

        // Assert
        Assert.True(result.Success);
        Assert.Contains(new ExecutedOperation(HostName, vm.Id, VmOperation.Start), factory.AgentClient.Executed);
        Assert.Equal(UserId, (string?)item!["userId"]);
        Assert.Equal(HostName, (string?)item["hostName"]);
        Assert.Equal(vm.Id, (string?)item["vmId"]);
        Assert.Equal("Start", (string?)item["operation"]);
        Assert.True((bool?)item["success"]);
        Assert.Null(item["message"]);
    }

    [Fact]
    public async Task FailedOperationIsRecordedWithMessage()
    {
        // Arrange
        var vm = CreateVm();
        factory.AgentClient.Failures[vm.Id] = new AgentException("invalid state");

        // Act
        var result = await ExecuteAsync(HostName, vm, VmOperation.Pause);
        var item = Assert.Single(await QueryHistoryAsync(vm.Name));

        // Assert
        Assert.False(result.Success);
        Assert.Equal("invalid state", result.Error);
        Assert.False((bool?)item!["success"]);
        Assert.Equal("invalid state", (string?)item["message"]);
    }

    [Fact]
    public async Task UnknownHostIsRecordedAsFailure()
    {
        // Arrange
        var vm = CreateVm();

        // Act
        var result = await ExecuteAsync("Unknown", vm, VmOperation.TurnOff);
        var item = Assert.Single(await QueryHistoryAsync(vm.Name));

        // Assert
        Assert.False(result.Success);
        Assert.DoesNotContain(factory.AgentClient.Executed, x => x.VmId == vm.Id);
        Assert.Equal("Unknown", (string?)item!["hostName"]);
        Assert.False((bool?)item["success"]);
    }

    [Fact]
    public async Task ExportReturnsCsv()
    {
        // Arrange
        var vm = CreateVm();
        await ExecuteAsync(HostName, vm, VmOperation.Save);
        var client = factory.CreateClient();
        await TestApplicationFactory.LoginAsync(client, TestContext.Current.CancellationToken);

        // Act
        var response = await client.GetAsync(new Uri("/api/operations/csv", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("ExecutedAt", content, StringComparison.Ordinal);
        Assert.Contains(vm.Name, content, StringComparison.Ordinal);
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static VmInfo CreateVm() =>
        new(Guid.NewGuid().ToString(), $"vm-{Guid.NewGuid():N}", VmState.Off, null, null, null, "10.0");

    private async Task<OperationResult> ExecuteAsync(string hostName, VmInfo vm, VmOperation operation)
    {
        var service = factory.Services.GetRequiredService<OperationService>();
        var provider = factory.Services.GetRequiredService<ApplicationServiceContextProvider>();
        using (provider.Begin(static () => new ServiceContext(DateTimeOffset.Now, UserId)))
        {
            return await service.ExecuteAsync(hostName, vm, operation, TestContext.Current.CancellationToken);
        }
    }

    private async Task<JsonArray> QueryHistoryAsync(string keyword)
    {
        var client = factory.CreateClient();
        await TestApplicationFactory.LoginAsync(client, TestContext.Current.CancellationToken);
        var response = await client.GetFromJsonAsync<JsonObject>(new Uri($"/api/operations?keyword={Uri.EscapeDataString(keyword)}", UriKind.Relative), TestContext.Current.CancellationToken);
        return response!["items"]!.AsArray();
    }
}
