namespace HyperManager.Contracts;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter<VmState>))]
public enum VmState
{
    Unknown,
    Running,
    Off,
    Stopping,
    Saved,
    Paused,
    Starting,
    Saving,
    Pausing,
    Resuming,
    Other
}
