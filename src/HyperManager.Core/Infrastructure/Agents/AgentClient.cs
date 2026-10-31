namespace HyperManager.Infrastructure.Agents;

using System.Net.Http.Json;
using System.Text.Json;

public sealed class AgentClient : IAgentClient
{
    public const string HttpClientName = "Agent";

    private readonly IHttpClientFactory httpClientFactory;

    private readonly TimeSpan queryTimeout;

    private readonly TimeSpan operationTimeout;

    public AgentClient(
        IHttpClientFactory httpClientFactory,
        AgentClientOption option)
    {
        this.httpClientFactory = httpClientFactory;
        queryTimeout = TimeSpan.FromSeconds(option.QueryTimeoutSeconds);
        operationTimeout = TimeSpan.FromSeconds(option.OperationTimeoutSeconds);
    }

    //--------------------------------------------------------------------------------
    // Query
    //--------------------------------------------------------------------------------

    public async ValueTask<HostSnapshot> GetSnapshotAsync(AgentHostOption host, CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(HttpMethod.Get, host, AgentApi.SnapshotPath, null, queryTimeout, cancellationToken);
        try
        {
            return await response.Content.ReadFromJsonAsync<HostSnapshot>(cancellationToken) ?? throw new AgentException("応答が空です");
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new AgentException($"応答を解析できません: {ex.Message}", ex);
        }
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    public async ValueTask ExecuteAsync(AgentHostOption host, string vmId, VmOperation operation, CancellationToken cancellationToken = default)
    {
        var body = operation == VmOperation.Shutdown ? new ShutdownRequest(false) : null;
        var response = await SendAsync(HttpMethod.Post, host, AgentApi.OperationPath(vmId, operation), body, operationTimeout, cancellationToken);
        response.Dispose();
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private async ValueTask<HttpResponseMessage> SendAsync(HttpMethod method, AgentHostOption host, string path, object? body, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, BuildUri(host.Url, path));
        request.Headers.Add(AgentApi.ApiKeyHeader, host.ApiKey);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType());
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        HttpResponseMessage response;
        try
        {
            using var client = httpClientFactory.CreateClient(HttpClientName);
            response = await client.SendAsync(request, cts.Token);
        }
        catch (HttpRequestException ex)
        {
            throw new AgentException($"接続に失敗しました: {ex.Message}", ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AgentException("応答がタイムアウトしました", ex);
        }

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            throw new AgentException(await ReadErrorMessageAsync(response, cancellationToken), response.StatusCode);
        }
    }

    private static Uri BuildUri(Uri baseUrl, string path)
    {
        // Keep the path of the base URL (e.g. prefix of a reverse proxy)
        var root = baseUrl.AbsoluteUri.EndsWith('/') ? baseUrl : new Uri(baseUrl.AbsoluteUri + "/");
        return new Uri(root, path);
    }

    private static async ValueTask<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return "認証に失敗しました (API キーを確認してください)";
        }

        // ProblemDetails
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (TryGetString(root, "detail", out var detail))
                {
                    return detail;
                }

                if (TryGetString(root, "title", out var title))
                {
                    return title;
                }
            }
        }
        catch (JsonException)
        {
            // Body is not a ProblemDetails payload; fall through to the status code message.
        }

        return $"エージェントがエラーを返しました (HTTP {(int)response.StatusCode})";
    }

    private static bool TryGetString(JsonElement element, string name, [NotNullWhen(true)] out string? value)
    {
        if (element.TryGetProperty(name, out var property) && (property.ValueKind == JsonValueKind.String))
        {
            value = property.GetString();
            return !String.IsNullOrEmpty(value);
        }

        value = null;
        return false;
    }
}
