namespace HyperManager.Services;

internal static partial class Log
{
    // Monitor

    [LoggerMessage(Level = LogLevel.Warning, Message = "Host query failed. host=[{host}], url=[{url}], message=[{message}]")]
    public static partial void WarnHostQueryFailed(this ILogger logger, string host, Uri url, string message);

    // Operation

    [LoggerMessage(Level = LogLevel.Information, Message = "VM operation succeeded. user=[{user}], host=[{host}], vm=[{vm}], operation=[{operation}]")]
    public static partial void InfoOperationSucceeded(this ILogger logger, string user, string host, string vm, VmOperation operation);

    [LoggerMessage(Level = LogLevel.Warning, Message = "VM operation failed. user=[{user}], host=[{host}], vm=[{vm}], operation=[{operation}], message=[{message}]")]
    public static partial void WarnOperationFailed(this ILogger logger, string user, string host, string vm, VmOperation operation, string? message);
}
