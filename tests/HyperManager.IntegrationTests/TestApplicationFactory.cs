namespace HyperManager;

using System.Text.RegularExpressions;

using HyperManager.Infrastructure.Agents;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

public sealed partial class TestApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseFile = $"test-{Guid.NewGuid():N}.db";

    // Replaces the HTTP client of the agents
    public StubAgentClient AgentClient { get; } = new();

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryTokenRegex();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("http_ports", string.Empty);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={databaseFile};Cache=Shared;Pooling=False");
        builder.UseSetting("Prometheus:Uri", string.Empty);
        builder.UseSetting("Monitor:Enable", "false");
        builder.UseSetting("Profiler:SqlLog:Enable", "false");
        builder.UseSetting("Profiler:SqlTelemetry:Enable", "false");
        builder.UseSetting("Log:HttpLog", "false");
        builder.ConfigureTestServices(services => services.AddSingleton<IAgentClient>(AgentClient));
    }

    // Cookie authentication: post the login form with the antiforgery token of the login page
    public static async Task LoginAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var loginPage = await client.GetStringAsync(new Uri("/login", UriKind.Relative), cancellationToken);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["name"] = "admin",
            ["password"] = "admin",
            ["__RequestVerificationToken"] = AntiforgeryTokenRegex().Match(loginPage).Groups[1].Value
        });
        using var response = await client.PostAsync(new Uri("/auth/login", UriKind.Relative), form, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(databaseFile))
        {
            try
            {
                File.Delete(databaseFile);
            }
            catch (IOException)
            {
                // Ignore
            }
        }
    }
}
