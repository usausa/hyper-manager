namespace HyperManager;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Net.Http.Headers;

public sealed class AuthenticationTests : IClassFixture<TestApplicationFactory>
{
    private const string AuthenticationCookie = ".AspNetCore.Cookies";

    private const string AntiforgeryCookie = ".AspNetCore.Antiforgery";

    private readonly TestApplicationFactory factory;

    public AuthenticationTests(TestApplicationFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task LoginCookieIsNotSecureInDevelopment()
    {
        // Act
        var cookie = await LoginAsync(factory);

        // Assert
        Assert.False(cookie.Secure);
    }

    [Fact]
    public async Task LoginCookieIsSecureInProduction()
    {
        // Arrange
        await using var production = factory.WithWebHostBuilder(static builder => builder.UseEnvironment("Production"));

        // Act
        var cookie = await LoginAsync(production);

        // Assert
        Assert.True(cookie.Secure);
    }

    [Fact]
    public async Task LoginCookieIsNotSecureWhenHttpIsAllowed()
    {
        // Arrange
        await using var production = factory.WithWebHostBuilder(static builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Auth:AllowHttp", "true");
        });

        // Act
        var cookie = await LoginAsync(production);

        // Assert
        Assert.False(cookie.Secure);
    }

    // Logs in without the cookie handling of the client to inspect the cookie returned by the server
    private static async Task<SetCookieHeaderValue> LoginAsync(WebApplicationFactory<Program> application)
    {
        var client = application.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        var loginPage = await client.GetAsync(new Uri("/login", UriKind.Relative), TestContext.Current.CancellationToken);
        var antiforgery = ParseCookies(loginPage).First(static x => x.Name.StartsWith(AntiforgeryCookie, StringComparison.Ordinal));
        client.DefaultRequestHeaders.Add(HeaderNames.Cookie, $"{antiforgery.Name}={antiforgery.Value}");

        using var form = TestApplicationFactory.CreateLoginForm(await loginPage.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var response = await client.PostAsync(new Uri("/auth/login", UriKind.Relative), form, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return ParseCookies(response).Single(static x => x.Name.Equals(AuthenticationCookie, StringComparison.Ordinal));
    }

    private static IList<SetCookieHeaderValue> ParseCookies(HttpResponseMessage response) =>
        SetCookieHeaderValue.ParseList(response.Headers.GetValues(HeaderNames.SetCookie).ToList());
}
