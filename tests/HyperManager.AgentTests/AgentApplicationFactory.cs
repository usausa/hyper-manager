namespace HyperManager;

using HyperManager.Agent.HyperV;
using HyperManager.Contracts;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

public sealed class AgentApplicationFactory : WebApplicationFactory<Program>
{
    public const string ApiKey = "test-api-key";

    // Replaces the CIM access
    public FakeHyperVService HyperV { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("http_ports", string.Empty);
        builder.UseSetting("Agent:ApiKey", ApiKey);
        builder.UseSetting("Prometheus:Uri", string.Empty);
        builder.UseSetting("Log:HttpLog", "false");
        builder.ConfigureTestServices(services => services.AddSingleton<IHyperVService>(HyperV));
    }

    public HttpClient CreateAuthorizedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(AgentApi.ApiKeyHeader, ApiKey);
        return client;
    }
}
