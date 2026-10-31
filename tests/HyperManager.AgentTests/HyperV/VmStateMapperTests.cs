namespace HyperManager.HyperV;

using HyperManager.Agent.HyperV;
using HyperManager.Contracts;

public sealed class VmStateMapperTests
{
    // Values of root\virtualization\v2 (same as Microsoft.HyperV.PowerShell.VMState)
    [Theory]
    [InlineData(2, VmState.Running)]
    [InlineData(3, VmState.Off)]
    [InlineData(4, VmState.Stopping)]
    [InlineData(6, VmState.Saved)]
    [InlineData(9, VmState.Paused)]
    [InlineData(10, VmState.Starting)]
    [InlineData(32773, VmState.Saving)]
    [InlineData(32776, VmState.Pausing)]
    [InlineData(32777, VmState.Resuming)]
    [InlineData(32779, VmState.Saved)]
    [InlineData(32780, VmState.Saving)]
    public void FromEnabledStateMapsKnownValue(int enabledState, VmState expected)
    {
        // Act
        var result = VmStateMapper.FromEnabledState((ushort)enabledState);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(32768)]
    [InlineData(32785)]
    public void FromEnabledStateMapsUnknownValueToOther(int enabledState)
    {
        // Act
        var result = VmStateMapper.FromEnabledState((ushort)enabledState);

        // Assert
        Assert.Equal(VmState.Other, result);
    }
}
