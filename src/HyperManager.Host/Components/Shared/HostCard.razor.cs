namespace HyperManager.Host.Components.Shared;

using Microsoft.AspNetCore.Components;

// Reachability, privilege and metrics of a host
public sealed partial class HostCard
{
    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    public required HostStatus Status { get; set; }
}
