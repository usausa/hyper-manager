namespace HyperManager.Agent.HyperV;

internal static class VmStateMapper
{
    // Maps the EnabledState value of root\virtualization\v2 (Msvm_SummaryInformation / Msvm_ComputerSystem) to VmState.
    // The values are the same as Microsoft.HyperV.PowerShell.VMState.
    public static VmState FromEnabledState(ushort enabledState) => enabledState switch
    {
        2 => VmState.Running,
        3 => VmState.Off,
        4 => VmState.Stopping,
        6 => VmState.Saved,
        9 => VmState.Paused,
        10 => VmState.Starting,
        11 => VmState.Starting,
        32773 => VmState.Saving,
        32776 => VmState.Pausing,
        32777 => VmState.Resuming,
        32779 => VmState.Saved,
        32780 => VmState.Saving,
        32781 => VmState.Stopping,
        32782 => VmState.Starting,
        _ => VmState.Other
    };
}
