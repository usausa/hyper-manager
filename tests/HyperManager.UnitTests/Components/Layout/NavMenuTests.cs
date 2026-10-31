namespace HyperManager.Components.Layout;

using Bunit;

using HyperManager.Host.Components.Layout;

public sealed class NavMenuTests : MudBlazorTestBase
{
    [Fact]
    public void RenderShowsNavigationLinks()
    {
        // Arrange & Act
        var cut = Render<NavMenu>();

        // Assert
        var links = cut.FindAll("a");
        Assert.Equal(2, links.Count);
    }
}
