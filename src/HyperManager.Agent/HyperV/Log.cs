namespace HyperManager.Agent.HyperV;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Counter unavailable. query=[{query}], message=[{message}]")]
    public static partial void DebugCounterUnavailable(this ILogger logger, string query, string message);
}
