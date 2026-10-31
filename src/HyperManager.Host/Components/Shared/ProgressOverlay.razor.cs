namespace HyperManager.Host.Components.Shared;

using Microsoft.AspNetCore.Components;

public sealed partial class ProgressOverlay
{
    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    public bool Visible { get; set; }

    [Parameter]
    public string? Message { get; set; }
}
