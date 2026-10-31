namespace HyperManager.Contracts;

// Routes and headers of the HyperManager.Agent API shared by the agent and its client.
// Paths are relative so that they can be combined with a base URL that has a path.
public static class AgentApi
{
    public const string ApiKeyHeader = "X-Api-Key";

    public const string HostPath = "api/host";

    public const string HostMetricsPath = "api/host/metrics";

    public const string SnapshotPath = "api/snapshot";

    public const string VmsPath = "api/vms";

    public static string OperationPath(string id, VmOperation operation) =>
        $"{VmsPath}/{Uri.EscapeDataString(id)}/{OperationName(operation)}";

    public static string OperationName(VmOperation operation) => operation switch
    {
        VmOperation.Start => "start",
        VmOperation.Resume => "resume",
        VmOperation.Pause => "pause",
        VmOperation.Save => "save",
        VmOperation.Shutdown => "shutdown",
        VmOperation.TurnOff => "turnoff",
        _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
    };
}
