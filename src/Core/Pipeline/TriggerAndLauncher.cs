using System.Diagnostics;
using System.Media;
using DanlangA_Bot.Core.Contracts;
using DanlangA_Bot.Core.Models;

namespace DanlangA_Bot.Core.Pipeline;

public sealed class TriggerActionPipeline
{
    private readonly IConfigManager _configManager;
    private readonly INotificationService _notificationService;

    public TriggerActionPipeline(IConfigManager configManager, INotificationService notificationService)
    {
        _configManager = configManager;
        _notificationService = notificationService;
    }

    public void FireTrigger(string triggerName)
    {
        var triggers = _configManager.CurrentConfig.Triggers;
        if (triggers.Count == 0) return;

        foreach (var t in triggers)
        {
            if (string.Equals(t.Trigger, triggerName, StringComparison.OrdinalIgnoreCase))
            {
                ExecuteAction(t.Action, t.Payload);
            }
        }
    }

    public void ExecuteAction(string actionName, Dictionary<string, string> payload)
    {
        switch (actionName.ToLowerInvariant())
        {
            case "displaytoast":
            case "notify":
                string text = payload.GetValueOrDefault("text", "Thông báo từ Pet Assistant");
                string mood = payload.GetValueOrDefault("mood", "happy");
                int.TryParse(payload.GetValueOrDefault("duration_ms", "4000"), out int duration);
                _notificationService.Notify(text, mood, duration <= 0 ? 4000 : duration);
                break;

            case "launchapp":
                if (payload.TryGetValue("target", out var target))
                {
                    string resolved = _configManager.ResolvePath(target);
                    string args = payload.GetValueOrDefault("arguments", "");
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = resolved,
                            Arguments = args,
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                    }
                    catch
                    {
                        // ponytail: [app launch failure] -> [logging / feedback]
                    }
                }
                break;

            case "playsound":
                if (payload.TryGetValue("sound_file", out var soundFile))
                {
                    string resolved = _configManager.ResolvePath(soundFile);
                    if (File.Exists(resolved))
                    {
                        Platform.Win32Interop.PlaySoundW(resolved, 0, Platform.Win32Interop.SND_ASYNC | Platform.Win32Interop.SND_FILENAME);
                    }
                    else
                    {
                        Platform.Win32Interop.MessageBeep(0);
                    }
                }
                else
                {
                    Platform.Win32Interop.MessageBeep(0);
                }
                break;

            case "executescript":
                if (payload.TryGetValue("script", out var script))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {script}",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        Process.Start(psi);
                    }
                    catch { }
                }
                break;
        }
    }
}

public sealed class StaggeredLauncher
{
    private readonly IConfigManager _configManager;

    public StaggeredLauncher(IConfigManager configManager)
    {
        _configManager = configManager;
    }

    public void LaunchAllStaggered()
    {
        var config = _configManager.CurrentConfig.StaggeredLauncher;
        if (config.Items.Count == 0) return;

        Task.Run(async () =>
        {
            foreach (var item in config.Items)
            {
                if (!item.Enabled || string.IsNullOrWhiteSpace(item.Target)) continue;

                try
                {
                    string resolved = _configManager.ResolvePath(item.Target);
                    var psi = new ProcessStartInfo
                    {
                        FileName = resolved,
                        Arguments = item.Arguments,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch
                {
                    // Non-fatal
                }

                if (config.LaunchDelayMs > 0)
                {
                    await Task.Delay(config.LaunchDelayMs);
                }
            }
        });
    }
}
