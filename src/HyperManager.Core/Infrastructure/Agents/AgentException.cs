namespace HyperManager.Infrastructure.Agents;

// Raised when an agent is unreachable or returns a failure.
public sealed class AgentException : Exception
{
    public AgentException()
    {
    }

    public AgentException(string message)
        : base(message)
    {
    }

    public AgentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public AgentException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    // HTTP status code when the agent returned an error response
    public HttpStatusCode? StatusCode { get; }
}
