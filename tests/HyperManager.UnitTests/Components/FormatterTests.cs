namespace HyperManager.Components;

using HyperManager.Contracts;
using HyperManager.Host.Components;

using MudBlazor;

public sealed class FormatterTests
{
    [Fact]
    public void FormatUptimeWithoutValueReturnsDash()
    {
        // Act
        var result = Formatter.FormatUptime(null);

        // Assert
        Assert.Equal("-", result);
    }

    [Fact]
    public void FormatUptimeLessThanDayReturnsTime()
    {
        // Act
        var result = Formatter.FormatUptime(new TimeSpan(2, 3, 4));

        // Assert
        Assert.Equal("02:03:04", result);
    }

    [Fact]
    public void FormatUptimeOverDayReturnsDaysAndTime()
    {
        // Act
        var result = Formatter.FormatUptime(new TimeSpan(1, 2, 3, 4));

        // Assert
        Assert.Equal("1d 02:03:04", result);
    }

    [Fact]
    public void MemoryUsedPercentWithoutTotalReturnsZero()
    {
        // Act
        var result = Formatter.MemoryUsedPercent(new HostMetrics(0, 0, 0, []));

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void DiskUsedPercentReturnsUsedRatio()
    {
        // Act
        var result = Formatter.DiskUsedPercent(new DiskInfo("C:", 200, 50));

        // Assert
        Assert.Equal(75, result);
    }

    [Theory]
    [InlineData(10, Color.Primary)]
    [InlineData(75, Color.Warning)]
    [InlineData(90, Color.Error)]
    public void UsageColorDependsOnThreshold(double percent, Color expected)
    {
        // Act
        var result = Formatter.UsageColor(percent);

        // Assert
        Assert.Equal(expected, result);
    }
}
