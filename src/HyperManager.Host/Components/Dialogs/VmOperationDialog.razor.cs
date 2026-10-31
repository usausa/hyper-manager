namespace HyperManager.Host.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// Confirmation before a VM operation
public sealed partial class VmOperationDialog
{
    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    public required string HostName { get; set; }

    [Parameter]
    public required string VmName { get; set; }

    [Parameter]
    public required VmOperation Operation { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    //--------------------------------------------------------------------------------
    // Property
    //--------------------------------------------------------------------------------

    private string Message => Operation switch
    {
        VmOperation.Start => $"仮想マシン「{VmName}」を開始しますか？",
        VmOperation.Resume => $"仮想マシン「{VmName}」を再開しますか？",
        VmOperation.Pause => $"仮想マシン「{VmName}」を一時停止しますか？",
        VmOperation.Save => $"仮想マシン「{VmName}」の状態を保存して停止しますか？",
        VmOperation.Shutdown => $"仮想マシン「{VmName}」をシャットダウンしますか？",
        VmOperation.TurnOff => $"仮想マシン「{VmName}」を強制的に電源オフしますか？",
        _ => $"仮想マシン「{VmName}」に{Operation.ToLabel()}を実行しますか？"
    };

    private string? Note => Operation switch
    {
        VmOperation.Shutdown => "ゲスト OS に正常終了を要求します (統合サービスが必要です)。",
        VmOperation.TurnOff => "ゲスト OS 内の未保存データは失われる可能性があります。",
        _ => null
    };

    private Severity NoteSeverity => Operation == VmOperation.TurnOff ? Severity.Warning : Severity.Info;

    private bool HasNote => Note is not null;

    //--------------------------------------------------------------------------------
    // Events
    //--------------------------------------------------------------------------------

    private void OnOkClick() => MudDialog.Close(DialogResult.Ok(true));

    private void OnCancelClick() => MudDialog.Cancel();
}
