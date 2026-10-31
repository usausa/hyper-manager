namespace HyperManager.Components;

using HyperManager.Host.Components;

public sealed class DisplayExtensionsTests
{
    [Fact]
    public void OperationLabelOfKnownOperationReturnsLabel()
    {
        // Act
        var result = "TurnOff".ToOperationLabel();

        // Assert
        Assert.Equal("強制停止", result);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("99")]
    public void OperationLabelOfUnknownOperationReturnsValue(string value)
    {
        // Act
        var result = value.ToOperationLabel();

        // Assert
        Assert.Equal(value, result);
    }
}
