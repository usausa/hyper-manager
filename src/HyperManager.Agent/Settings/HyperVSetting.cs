namespace HyperManager.Agent.Settings;

public sealed class HyperVSetting
{
    // Maximum time to wait for a state change job
    [Range(1, 3600)]
    public int JobTimeoutSeconds { get; set; } = 300;

    [Range(50, 10_000)]
    public int JobPollIntervalMilliseconds { get; set; } = 200;
}
