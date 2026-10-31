namespace HyperManager.Domain;

using HyperManager.Contracts;

public sealed class VmOperationPolicyTests
{
    [Theory]
    [InlineData(VmState.Off, VmOperation.Start, true)]
    [InlineData(VmState.Saved, VmOperation.Start, true)]
    [InlineData(VmState.Running, VmOperation.Start, false)]
    [InlineData(VmState.Paused, VmOperation.Resume, true)]
    [InlineData(VmState.Running, VmOperation.Resume, false)]
    [InlineData(VmState.Running, VmOperation.Pause, true)]
    [InlineData(VmState.Running, VmOperation.Save, true)]
    [InlineData(VmState.Paused, VmOperation.Save, false)]
    [InlineData(VmState.Running, VmOperation.Shutdown, true)]
    [InlineData(VmState.Off, VmOperation.Shutdown, false)]
    [InlineData(VmState.Running, VmOperation.TurnOff, true)]
    [InlineData(VmState.Paused, VmOperation.TurnOff, true)]
    [InlineData(VmState.Saved, VmOperation.TurnOff, false)]
    public void CanExecuteDependsOnState(VmState state, VmOperation operation, bool expected)
    {
        // Act
        var result = VmOperationPolicy.CanExecute(state, operation);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void AvailableOperationsOfRunningVm()
    {
        // Act
        var result = VmOperationPolicy.AvailableOperations(VmState.Running);

        // Assert
        Assert.Equal([VmOperation.Pause, VmOperation.Save, VmOperation.Shutdown, VmOperation.TurnOff], result);
    }

    [Theory]
    [InlineData(VmState.Starting)]
    [InlineData(VmState.Stopping)]
    [InlineData(VmState.Saving)]
    [InlineData(VmState.Pausing)]
    [InlineData(VmState.Resuming)]
    [InlineData(VmState.Other)]
    [InlineData(VmState.Unknown)]
    public void AvailableOperationsOfTransitionalStateIsEmpty(VmState state)
    {
        // Act
        var result = VmOperationPolicy.AvailableOperations(state);

        // Assert
        Assert.Empty(result);
    }
}
