namespace HyperManager;

using Microsoft.Playwright;

public sealed class VmOperationTests : E2ETestBase
{
    [Fact]
    public async Task DashboardShowsHostAndVirtualMachine()
    {
        // Given
        await using var factory = new E2EApplicationFactory();
        factory.UseKestrel(0);
        factory.StartServer();

        // When
        await LoginAsync(factory);

        // Then
        await Expect(Page.GetByText(StubAgentClient.MachineName)).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Cell, new PageGetByRoleOptions { Name = StubAgentClient.VmName })).ToBeVisibleAsync();
        await Expect(Page.GetByText("ディスク C:")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task StartIsConfirmedAndRecordedInHistory()
    {
        // Given
        await using var factory = new E2EApplicationFactory();
        factory.UseKestrel(0);
        factory.StartServer();
        await LoginAsync(factory);

        // When
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "開始", Exact = true }).ClickAsync();
        var dialog = Page.Locator(".mud-dialog");
        await Expect(dialog.GetByText($"仮想マシン「{StubAgentClient.VmName}」を開始しますか？")).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "開始", Exact = true }).ClickAsync();

        // Then
        await Expect(Page.GetByText($"開始しました: {StubAgentClient.VmName}")).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = "操作履歴" }).ClickAsync();
        var row = Page.GetByRole(AriaRole.Row).Filter(new LocatorFilterOptions { HasText = StubAgentClient.VmName });
        await Expect(row).ToContainTextAsync("開始");
        await Expect(row).ToContainTextAsync("成功");
        await Expect(row).ToContainTextAsync("admin");
    }

    private async Task LoginAsync(E2EApplicationFactory factory)
    {
        await Page.GotoAsync(factory.ServerAddress + "/login");
        await Page.FillAsync("#name", "admin");
        await Page.FillAsync("#password", "admin");
        await Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "ログイン" }).ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "仮想マシン" })).ToBeVisibleAsync();
    }
}
