namespace HyperManager.Components;

using Bunit;

using HyperManager.Contracts;
using HyperManager.Host.Components.Shared;
using HyperManager.Models;

using MudBlazor;

public sealed class HostCardTests : MudBlazorTestBase
{
    private const long Gigabyte = 1024L * 1024 * 1024;

    [Fact]
    public void RenderShowsMetricsOfReachableHost()
    {
        // Arrange
        var metrics = new HostMetrics(25, 16384, 8192, [new DiskInfo("C:", 100 * Gigabyte, 40 * Gigabyte)]);
        var status = new HostStatus("Host A", new HostInfo("PC-A", true), metrics, [], null);

        // Act
        var cut = Render<HostCard>(parameters => parameters.Add(static x => x.Status, status));

        // Assert
        Assert.Contains("Host A", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("PC-A", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("管理者", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("非管理者", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("25%", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("ディスク C:", cut.Markup, StringComparison.Ordinal);
        Assert.Equal(3, cut.FindAll(".mud-progress-linear").Count);
    }

    [Fact]
    public void RenderShowsWarningOfNonElevatedHost()
    {
        // Arrange
        Render<MudPopoverProvider>();
        var status = new HostStatus("Host A", new HostInfo("PC-A", false), null, [], null);

        // Act
        var cut = Render<HostCard>(parameters => parameters.Add(static x => x.Status, status));

        // Assert
        Assert.Contains("非管理者", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".mud-progress-linear"));
    }

    [Fact]
    public void RenderShowsErrorOfUnreachableHost()
    {
        // Arrange
        var status = new HostStatus("Host B", null, null, [], "接続に失敗しました");

        // Act
        var cut = Render<HostCard>(parameters => parameters.Add(static x => x.Status, status));

        // Assert
        Assert.Contains("到達不可", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("接続に失敗しました", cut.Markup, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll(".mud-progress-linear"));
    }
}
