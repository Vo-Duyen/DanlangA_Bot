using System.Text.Json.Serialization;

namespace DanlangA_Bot.Core.Models;

public sealed class AppConfig
{
    [JsonPropertyName("pet")]
    public PetConfig Pet { get; set; } = new();

    [JsonPropertyName("window")]
    public WindowConfig Window { get; set; } = new();

    [JsonPropertyName("autostart")]
    public AutostartConfig Autostart { get; set; } = new();

    [JsonPropertyName("ipc")]
    public IpcConfig Ipc { get; set; } = new();

    [JsonPropertyName("occlusion")]
    public OcclusionConfig Occlusion { get; set; } = new();

    [JsonPropertyName("speech_bubble")]
    public SpeechBubbleConfig SpeechBubble { get; set; } = new();

    [JsonPropertyName("staggered_launcher")]
    public StaggeredLauncherConfig StaggeredLauncher { get; set; } = new();

    [JsonPropertyName("triggers")]
    public List<TriggerActionConfig> Triggers { get; set; } = [];
}

public sealed class PetConfig
{
    [JsonPropertyName("asset_dir")]
    public string AssetDirectory { get; set; } = Path.Combine("assets", "rem--l1");

    [JsonPropertyName("manifest_file")]
    public string ManifestFile { get; set; } = "pet.json";
}

public sealed class WindowConfig
{
    [JsonPropertyName("relative_x_percent")]
    public double RelativeXPercent { get; set; } = 85.0;

    [JsonPropertyName("relative_y_percent")]
    public double RelativeYPercent { get; set; } = 75.0;

    [JsonPropertyName("scale")]
    public double Scale { get; set; } = 1.0;

    [JsonPropertyName("min_scale")]
    public double MinScale { get; set; } = 0.5;

    [JsonPropertyName("max_scale")]
    public double MaxScale { get; set; } = 3.0;

    [JsonPropertyName("click_through")]
    public bool ClickThrough { get; set; } = false;

    [JsonPropertyName("topmost")]
    public bool Topmost { get; set; } = true;

    [JsonPropertyName("active_fps")]
    public int ActiveFps { get; set; } = 15;
}

public sealed class AutostartConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("registry_key_name")]
    public string RegistryKeyName { get; set; } = "DanlangDesktopPet";
}

public sealed class IpcConfig
{
    [JsonPropertyName("pipe_name")]
    public string PipeName { get; set; } = "pet_assistant_ipc";
}

public sealed class OcclusionConfig
{
    [JsonPropertyName("fullscreen_snooze")]
    public bool FullscreenSnooze { get; set; } = true;

    [JsonPropertyName("check_interval_ms")]
    public int CheckIntervalMs { get; set; } = 2000;
}

public sealed class SpeechBubbleConfig
{
    [JsonPropertyName("default_duration_ms")]
    public int DefaultDurationMs { get; set; } = 4000;

    [JsonPropertyName("max_width")]
    public int MaxWidth { get; set; } = 240;

    [JsonPropertyName("font_size")]
    public int FontSize { get; set; } = 13;
}

public sealed class StaggeredLauncherConfig
{
    [JsonPropertyName("launch_delay_ms")]
    public int LaunchDelayMs { get; set; } = 1500;

    [JsonPropertyName("items")]
    public List<LauncherItem> Items { get; set; } = [];
}

public sealed class LauncherItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

public sealed class TriggerActionConfig
{
    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public Dictionary<string, string> Payload { get; set; } = [];
}

public sealed class PetManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("spritesheetPath")]
    public string SpritesheetPath { get; set; } = "spritesheet.webp";
}
