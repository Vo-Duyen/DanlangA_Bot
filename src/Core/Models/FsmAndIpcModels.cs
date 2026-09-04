using System.Text.Json.Serialization;

namespace DanlangA_Bot.Core.Models;

public sealed class FsmConfig
{
    [JsonPropertyName("initial_state")]
    public string InitialState { get; set; } = "idle";

    [JsonPropertyName("states")]
    public Dictionary<string, StateDefinition> States { get; set; } = [];
}

public sealed class StateDefinition
{
    [JsonPropertyName("fps")]
    public int Fps { get; set; } = 0;

    [JsonPropertyName("frame_count")]
    public int FrameCount { get; set; } = 1;

    [JsonPropertyName("atlas_row")]
    public int AtlasRow { get; set; } = 0;

    [JsonPropertyName("loop")]
    public bool Loop { get; set; } = true;

    [JsonPropertyName("min_duration_ms")]
    public int MinDurationMs { get; set; } = 3000;

    [JsonPropertyName("max_duration_ms")]
    public int MaxDurationMs { get; set; } = 6000;

    [JsonPropertyName("transitions")]
    public List<StateTransition> Transitions { get; set; } = [];
}

public sealed class StateTransition
{
    [JsonPropertyName("target")]
    public string Target { get; set; } = "idle";

    [JsonPropertyName("weight")]
    public int Weight { get; set; } = 100;
}

public sealed class IpcMessage
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public IpcNotificationPayload? Payload { get; set; }
}

public sealed class IpcNotificationPayload
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("mood")]
    public string Mood { get; set; } = "happy";

    [JsonPropertyName("duration_ms")]
    public int DurationMs { get; set; } = 4000;
}
