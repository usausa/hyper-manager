namespace HyperManager.Agent.Endpoints;

internal static partial class Log
{
    // Snapshot

    [LoggerMessage(Level = LogLevel.Warning, Message = "Host metrics unavailable. Returning snapshot without metrics.")]
    public static partial void WarnMetricsUnavailable(this ILogger logger, Exception ex);

    // Operation

    [LoggerMessage(Level = LogLevel.Information, Message = "VM operation succeeded. operation=[{operation}], id=[{id}]")]
    public static partial void InfoOperationSucceeded(this ILogger logger, VmOperation operation, string id);

    [LoggerMessage(Level = LogLevel.Warning, Message = "VM operation failed. operation=[{operation}], id=[{id}], message=[{message}]")]
    public static partial void WarnOperationFailed(this ILogger logger, VmOperation operation, string id, string message);
}
