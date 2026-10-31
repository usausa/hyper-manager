namespace HyperManager.Domain;

// Determines which operations are available for a given VM state.
// No operation is available while the VM is in a transitional state.
public static class VmOperationPolicy
{
    private static readonly VmOperation[] Operations = Enum.GetValues<VmOperation>();

    public static bool CanExecute(VmState state, VmOperation operation) => operation switch
    {
        VmOperation.Start => state is VmState.Off or VmState.Saved,
        VmOperation.Resume => state is VmState.Paused,
        VmOperation.Pause => state is VmState.Running,
        VmOperation.Save => state is VmState.Running,
        VmOperation.Shutdown => state is VmState.Running,
        VmOperation.TurnOff => state is VmState.Running or VmState.Paused,
        _ => false
    };

    public static IEnumerable<VmOperation> AvailableOperations(VmState state) =>
        Operations.Where(x => CanExecute(state, x));
}
