namespace HyperManager.Host.Components;

using MudBlazor;

// Labels, colors and icons of the domain values on the screen
public static class DisplayExtensions
{
    //--------------------------------------------------------------------------------
    // VM state
    //--------------------------------------------------------------------------------

    public static string ToLabel(this VmState state) => state switch
    {
        VmState.Running => "実行中",
        VmState.Off => "停止",
        VmState.Paused => "一時停止",
        VmState.Saved => "保存済み",
        VmState.Starting => "起動中",
        VmState.Stopping => "停止中",
        VmState.Saving => "保存中",
        VmState.Pausing => "一時停止中",
        VmState.Resuming => "再開中",
        _ => "不明"
    };

    public static Color ToColor(this VmState state) => state switch
    {
        VmState.Running => Color.Success,
        VmState.Off => Color.Default,
        VmState.Paused => Color.Warning,
        VmState.Pausing => Color.Warning,
        VmState.Saved => Color.Info,
        VmState.Saving => Color.Info,
        VmState.Starting => Color.Info,
        VmState.Stopping => Color.Info,
        VmState.Resuming => Color.Info,
        _ => Color.Secondary
    };

    //--------------------------------------------------------------------------------
    // VM operation
    //--------------------------------------------------------------------------------

    public static string ToLabel(this VmOperation operation) => operation switch
    {
        VmOperation.Start => "開始",
        VmOperation.Resume => "再開",
        VmOperation.Pause => "一時停止",
        VmOperation.Save => "状態保存",
        VmOperation.Shutdown => "シャットダウン",
        VmOperation.TurnOff => "強制停止",
        _ => operation.ToString()
    };

    public static Color ToColor(this VmOperation operation) => operation switch
    {
        VmOperation.Start => Color.Success,
        VmOperation.Resume => Color.Success,
        VmOperation.Pause => Color.Default,
        VmOperation.Save => Color.Info,
        VmOperation.Shutdown => Color.Warning,
        VmOperation.TurnOff => Color.Error,
        _ => Color.Default
    };

    public static string ToIcon(this VmOperation operation) => operation switch
    {
        VmOperation.Start => Icons.Material.Filled.PlayArrow,
        VmOperation.Resume => Icons.Material.Filled.PlayArrow,
        VmOperation.Pause => Icons.Material.Filled.Pause,
        VmOperation.Save => Icons.Material.Filled.Save,
        VmOperation.Shutdown => Icons.Material.Filled.PowerSettingsNew,
        VmOperation.TurnOff => Icons.Material.Filled.Stop,
        _ => Icons.Material.Filled.QuestionMark
    };

    public static string ToCompletedMessage(this VmOperation operation, string vmName) => operation switch
    {
        VmOperation.Start => $"開始しました: {vmName}",
        VmOperation.Resume => $"再開しました: {vmName}",
        VmOperation.Pause => $"一時停止しました: {vmName}",
        VmOperation.Save => $"状態を保存しました: {vmName}",
        VmOperation.Shutdown => $"シャットダウンを要求しました: {vmName}",
        VmOperation.TurnOff => $"強制停止しました: {vmName}",
        _ => $"{operation.ToLabel()}しました: {vmName}"
    };

    // Operation name stored in the history
    public static string ToOperationLabel(this string operation) =>
        Enum.TryParse<VmOperation>(operation, out var value) && Enum.IsDefined(value) ? value.ToLabel() : operation;
}
