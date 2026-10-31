namespace HyperManager.Host.Components.Pages;

using HyperManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

using MudBlazor;

public sealed partial class Home
{
    private static readonly int[] PageSizeOptions = [25, 50, 100];

    private MonitorSnapshot snapshot = MonitorSnapshot.Empty;

    private bool loading;

    private bool running;

    private bool canOperate;

    private string? errorMessage;

    // Selected host. An empty string means all hosts
    private string hostFilter = string.Empty;

    private string? nameFilter;

    private bool HasError => !String.IsNullOrEmpty(errorMessage);

    private bool NoHosts => (snapshot.UpdatedAt is not null) && (snapshot.Hosts.Count == 0);

    private bool HasMultipleHosts => snapshot.Hosts.Count > 1;

    // Flattened and filtered rows shown in the grid
    private List<VmRow> Rows =>
        snapshot.Hosts
            .Where(x => (hostFilter.Length == 0) || String.Equals(x.Name, hostFilter, StringComparison.Ordinal))
            .SelectMany(static x => x.Vms.Select(vm => new VmRow(x.Name, vm)))
            .Where(x => String.IsNullOrWhiteSpace(nameFilter) || x.Vm.Name.Contains(nameFilter.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    [Inject]
    public required MonitorService MonitorService { get; set; }

    [Inject]
    public required OperationService OperationService { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [CascadingParameter]
    public required Task<AuthenticationState> AuthenticationState { get; set; }

    //--------------------------------------------------------------------------------
    // Initialize
    //--------------------------------------------------------------------------------

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthenticationState;
        canOperate = state.User.IsInRole(Roles.Administrator);

        MonitorService.Updated += OnMonitorUpdated;

        // Show the latest snapshot immediately instead of waiting for the next refresh
        snapshot = MonitorService.Current;
        if (snapshot.UpdatedAt is null)
        {
            await RefreshAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            MonitorService.Updated -= OnMonitorUpdated;
        }

        base.Dispose(disposing);
    }

    //--------------------------------------------------------------------------------
    // Event
    //--------------------------------------------------------------------------------

    private void OnMonitorUpdated(object? sender, MonitorEventArgs e)
    {
        _ = InvokeAsync(() =>
        {
            snapshot = e.Snapshot;
            StateHasChanged();
        });
    }

    private void ClearError() => errorMessage = null;

    //--------------------------------------------------------------------------------
    // Action
    //--------------------------------------------------------------------------------

    private async Task RefreshAsync()
    {
        loading = true;
        try
        {
            // Every open page (including this one) is updated by the event
            await MonitorService.RefreshAsync();
        }
        finally
        {
            loading = false;
        }
    }

    private async Task ExecuteAsync(VmRow row, VmOperation operation)
    {
        if (!await DialogService.ShowOperationConfirm(row.HostName, row.Vm.Name, operation))
        {
            return;
        }

        running = true;
        errorMessage = null;
        StateHasChanged();
        try
        {
            var result = await OperationService.ExecuteAsync(row.HostName, row.Vm, operation);
            if (result.Success)
            {
                Snackbar.AddSuccess(operation.ToCompletedMessage(row.Vm.Name));
            }
            else
            {
                errorMessage = $"{operation.ToLabel()}に失敗しました: {row.Vm.Name}: {result.Error}";
                Snackbar.AddError(errorMessage);
            }

            await MonitorService.RefreshAsync();
        }
        finally
        {
            running = false;
        }
    }

    //--------------------------------------------------------------------------------
    // Model
    //--------------------------------------------------------------------------------

    private sealed record VmRow(string HostName, VmInfo Vm);
}
