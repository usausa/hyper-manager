namespace HyperManager.Agent.Settings;

public sealed class AgentSetting
{
    // Value of the X-Api-Key header required by the API
    [Required]
    public string ApiKey { get; set; } = default!;
}
