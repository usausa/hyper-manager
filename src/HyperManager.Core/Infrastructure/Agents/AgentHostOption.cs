namespace HyperManager.Infrastructure.Agents;

// A HyperManager.Agent endpoint registered in configuration.
public sealed class AgentHostOption
{
    // Display name. Must be unique.
    public string Name { get; set; } = default!;

    public Uri Url { get; set; } = default!;

    public string ApiKey { get; set; } = default!;
}
