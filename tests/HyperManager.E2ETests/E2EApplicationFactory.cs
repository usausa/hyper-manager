namespace HyperManager;

using HyperManager.Infrastructure.Agents;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

public sealed class E2EApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string databaseFile = $"e2e-{Guid.NewGuid():N}.db";

    public string ServerAddress
    {
        get
        {
            var server = Services.GetRequiredService<IServer>();
            return server.Features.Get<IServerAddressesFeature>()!.Addresses.First();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("http_ports", string.Empty);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={databaseFile};Cache=Shared;Pooling=False");
        builder.UseSetting("Prometheus:Uri", string.Empty);
        builder.UseSetting("Monitor:Enable", "false");
        builder.UseSetting("Profiler:SqlLog:Enable", "false");
        builder.UseSetting("Profiler:SqlTelemetry:Enable", "false");
        builder.UseSetting("Log:HttpLog", "false");
        builder.ConfigureTestServices(static services => services.AddSingleton<IAgentClient, StubAgentClient>());
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
