namespace HyperManager.Infrastructure.Agents;

using System.Net;
using System.Net.Http;
using System.Text;

using HyperManager.Contracts;

using NSubstitute;

public sealed class AgentClientTests
{
    private static readonly AgentHostOption Host = new() { Name = "A", Url = new Uri("http://host-a:5100/agent"), ApiKey = "secret" };

    [Fact]
    public async Task GetSnapshotSendsApiKeyAndParsesResponse()
    {
        // Arrange
        using var handler = new StubHandler(static _ => CreateResponse(HttpStatusCode.OK, """{"host":{"name":"PC-A","isElevated":true},"vms":[{"id":"1","name":"vm-1","state":"Running","cpuUsagePercent":5,"uptime":"01:00:00"}]}"""));
        var client = CreateClient(handler);

        // Act
        var snapshot = await client.GetSnapshotAsync(Host, TestContext.Current.CancellationToken);

        // Assert
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(new Uri("http://host-a:5100/agent/api/snapshot"), request.Uri);
        Assert.Equal("secret", request.ApiKey);
        Assert.Equal("PC-A", snapshot.Host.Name);
        Assert.True(snapshot.Host.IsElevated);
        Assert.Null(snapshot.Metrics);
        var vm = Assert.Single(snapshot.Vms);
        Assert.Equal(VmState.Running, vm.State);
        Assert.Equal(5, vm.CpuUsagePercent);
        Assert.Equal(TimeSpan.FromHours(1), vm.Uptime);
    }

    [Fact]
    public async Task ExecuteSendsOperation()
    {
        // Arrange
        using var handler = new StubHandler(static _ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        // Act
        await client.ExecuteAsync(Host, "vm-id", VmOperation.Pause, TestContext.Current.CancellationToken);

        // Assert
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(new Uri("http://host-a:5100/agent/api/vms/vm-id/pause"), request.Uri);
        Assert.Null(request.Body);
    }

    [Fact]
    public async Task ExecuteShutdownSendsRequestBody()
    {
        // Arrange
        using var handler = new StubHandler(static _ => new HttpResponseMessage(HttpStatusCode.NoContent));
        var client = CreateClient(handler);

        // Act
        await client.ExecuteAsync(Host, "vm-id", VmOperation.Shutdown, TestContext.Current.CancellationToken);

        // Assert
        var request = Assert.Single(handler.Requests);
        Assert.Equal(new Uri("http://host-a:5100/agent/api/vms/vm-id/shutdown"), request.Uri);
        Assert.Equal("""{"force":false}""", request.Body);
    }

    [Fact]
    public async Task ExecuteFailureThrowsProblemDetail()
    {
        // Arrange
        using var handler = new StubHandler(static _ => CreateResponse(HttpStatusCode.Conflict, """{"title":"Conflict","detail":"invalid state"}"""));
        var client = CreateClient(handler);

        // Act
        var ex = await Assert.ThrowsAsync<AgentException>(async () => await client.ExecuteAsync(Host, "vm-id", VmOperation.Start, TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("invalid state", ex.Message);
        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
    }

    [Fact]
    public async Task UnauthorizedThrowsAuthenticationError()
    {
        // Arrange
        using var handler = new StubHandler(static _ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(handler);

        // Act
        var ex = await Assert.ThrowsAsync<AgentException>(async () => await client.GetSnapshotAsync(Host, TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Contains("API キー", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectionFailureThrowsAgentException()
    {
        // Arrange
        using var handler = new StubHandler(static _ => throw new HttpRequestException("refused"));
        var client = CreateClient(handler);

        // Act
        var ex = await Assert.ThrowsAsync<AgentException>(async () => await client.GetSnapshotAsync(Host, TestContext.Current.CancellationToken));

        // Assert
        Assert.IsType<HttpRequestException>(ex.InnerException);
    }

    [Fact]
    public async Task SlowResponseThrowsTimeout()
    {
        // Arrange
        using var handler = new StubHandler(static async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var client = CreateClient(handler, new AgentClientOption { QueryTimeoutSeconds = 1 });

        // Act
        var ex = await Assert.ThrowsAsync<AgentException>(async () => await client.GetSnapshotAsync(Host, TestContext.Current.CancellationToken));

        // Assert
        Assert.IsAssignableFrom<OperationCanceledException>(ex.InnerException);
    }

    [Fact]
    public async Task InvalidResponseThrowsAgentException()
    {
        // Arrange
        using var handler = new StubHandler(static _ => CreateResponse(HttpStatusCode.OK, "not json"));
        var client = CreateClient(handler);

        // Act & Assert
        await Assert.ThrowsAsync<AgentException>(async () => await client.GetSnapshotAsync(Host, TestContext.Current.CancellationToken));
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static AgentClient CreateClient(HttpMessageHandler handler, AgentClientOption? option = null)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(AgentClient.HttpClientName).Returns(_ => new HttpClient(handler, false));
        return new AgentClient(factory, option ?? new AgentClientOption());
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed record CapturedRequest(HttpMethod Method, Uri? Uri, string? ApiKey, string? Body);

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder;

        public List<CapturedRequest> Requests { get; } = [];

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            this.responder = (request, _) => Task.FromResult(responder(request));
        }

        public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        {
            this.responder = responder;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var apiKey = request.Headers.TryGetValues(AgentApi.ApiKeyHeader, out var values) ? values.Single() : null;
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.Method, request.RequestUri, apiKey, body));
            return await responder(request, cancellationToken);
        }
    }
}
