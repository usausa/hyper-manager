namespace HyperManager;

using System.Text.Json.Nodes;

public sealed class HostTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory factory;

    public HostTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task HealthReturnsOk()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task RootWithoutAuthShowsLoginPage()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Contains("login", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApiWithoutAuthReturnsUnauthorized()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync(new Uri("/api/operations/", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApiDocumentDescribesResponses()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var document = JsonNode.Parse(await client.GetStringAsync(new Uri("/openapi/v1.json", UriKind.Relative), TestContext.Current.CancellationToken))!;
        var operation = document["paths"]!["/api/operations"]!["get"]!;

        // Assert
        Assert.Equal("OperationList", (string?)operation["operationId"]);
        Assert.Equal("#/components/schemas/OperationListResponse", (string?)operation["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["$ref"]);
        Assert.NotNull(operation["responses"]!["401"]!["content"]!["application/problem+json"]);
        Assert.Equal("boolean", (string?)document["components"]!["schemas"]!["OperationListEntry"]!["properties"]!["success"]!["type"]);
    }
}
