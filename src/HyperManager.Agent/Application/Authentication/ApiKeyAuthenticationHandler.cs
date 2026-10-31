namespace HyperManager.Agent.Application.Authentication;

using System.Security.Cryptography;
using System.Text.Encodings.Web;

// Authenticates requests by the X-Api-Key header
public sealed class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";

    private const string ClientName = "agent-client";

    private readonly byte[] apiKey;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AgentSetting setting)
        : base(options, logger, encoder)
    {
        apiKey = Encoding.UTF8.GetBytes(setting.ApiKey);
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AgentApi.ApiKeyHeader, out var values))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Constant time comparison
        var provided = Encoding.UTF8.GetBytes(values.ToString());
        if (!CryptographicOperations.FixedTimeEquals(provided, apiKey))
        {
            Logger.WarnInvalidApiKey(Context.Connection.RemoteIpAddress?.ToString());
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, ClientName)], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
