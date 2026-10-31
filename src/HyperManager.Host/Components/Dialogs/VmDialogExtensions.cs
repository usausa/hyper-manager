namespace HyperManager.Host.Components.Dialogs;

using MudBlazor;

public static class VmDialogExtensions
{
    public static async ValueTask<bool> ShowOperationConfirm(this IDialogService dialog, string hostName, string vmName, VmOperation operation)
    {
        var reference = await dialog.ShowAsync<VmOperationDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(VmOperationDialog.HostName), hostName },
                { nameof(VmOperationDialog.VmName), vmName },
                { nameof(VmOperationDialog.Operation), operation }
            });
        var result = await reference.Result;
        return result is { Canceled: false };
    }
}
