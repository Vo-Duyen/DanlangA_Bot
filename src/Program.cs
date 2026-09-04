using System.Text.Json;
using DanlangA_Bot.Core.Config;
using DanlangA_Bot.Core.Contracts;
using DanlangA_Bot.Core.Fsm;
using DanlangA_Bot.Core.Ipc;
using DanlangA_Bot.Core.Models;
using DanlangA_Bot.Core.Pipeline;
using DanlangA_Bot.Platform;
using DanlangA_Bot.Rendering;

namespace DanlangA_Bot;

public static class Program
{
    private const string AppMutexName = "Global\\DanlangDesktopPetMutex";

    [STAThread]
    public static void Main(string[] args)
    {
        // 1. Single Instance Protection
        using var mutex = new Mutex(true, AppMutexName, out bool createdNew);
        if (!createdNew)
        {
            // Another instance is already running; notify via IPC if arguments passed
            return;
        }

        // 2. Load Configurations
        string baseDir = AppContext.BaseDirectory;
        string configPath = Path.Combine(baseDir, "config", "config.json");
        string statesPath = Path.Combine(baseDir, "config", "states.json");

        var configManager = new ConfigManager();
        configManager.Load(configPath);
        var appConfig = configManager.CurrentConfig;

        // 3. Self-healing Autostart check (HKCU)
        if (appConfig.Autostart.Enabled)
        {
            AutostartManager.EnsureSelfHealingAutostart(appConfig.Autostart.RegistryKeyName, true);
        }

        // 4. Initialize Domain & FSM Engine
        var fsm = new PetFsmEngine();
        fsm.Initialize(statesPath);

        // 5. Initialize Win32 Presentation
        var petWindow = new PetWindow(appConfig, fsm);
        petWindow.Initialize();

        // Notification bridge
        var notificationBridge = new ActionNotificationService((text, mood, duration) =>
        {
            petWindow.ShowNotification(text, mood, duration);
        });

        // 6. Initialize Rendering & 0 FPS Idle Animation Controller
        string petAssetDir = configManager.ResolvePath(appConfig.Pet.AssetDirectory);
        string petManifestPath = Path.Combine(petAssetDir, appConfig.Pet.ManifestFile);
        string spritesheetPath = ResolvePetSpritesheetPath(petAssetDir, petManifestPath);
        var spriteRenderer = new SpriteRenderer(spritesheetPath);
        var animationController = new AnimationController(petWindow, fsm, spriteRenderer);
        petWindow.OnScaleChanged += () => animationController.OnScaleChanged();
        animationController.Start();

        // 7. Initialize Smart Occlusion Detector (Fullscreen Snooze)
        using var occlusionDetector = new OcclusionDetector(appConfig.Occlusion, petWindow.Handle);
        occlusionDetector.OnOcclusionChanged += isOccluded =>
        {
            animationController.SetFrozen(isOccluded);
        };
        occlusionDetector.Start();

        // 8. Initialize Trigger-Action Pipeline & Staggered Launcher
        var triggerPipeline = new TriggerActionPipeline(configManager, notificationBridge);
        var staggeredLauncher = new StaggeredLauncher(configManager);

        triggerPipeline.FireTrigger("OnStartup");
        staggeredLauncher.LaunchAllStaggered();

        // 9. Initialize IPC Named Pipe Server
        using var cts = new CancellationTokenSource();
        var ipcServer = new NamedPipeIpcServer();
        ipcServer.OnMessageReceived += msg =>
        {
            if (string.Equals(msg.Event, "notify", StringComparison.OrdinalIgnoreCase) && msg.Payload != null)
            {
                petWindow.ShowNotification(msg.Payload.Text, msg.Payload.Mood, msg.Payload.DurationMs);
            }
            else if (string.Equals(msg.Event, "trigger", StringComparison.OrdinalIgnoreCase) && msg.Payload != null)
            {
                triggerPipeline.ExecuteAction(msg.Payload.Mood, new Dictionary<string, string> { { "text", msg.Payload.Text } });
            }
        };
        ipcServer.StartAsync(appConfig.Ipc.PipeName, cts.Token);

        // 10. Run Native Win32 Message Loop (Zero CPU Idle blocking GetMessageW)
        petWindow.RunMessageLoop();

        // 11. Cleanup & Save State
        cts.Cancel();
        configManager.Save(configPath);
        animationController.Dispose();
        spriteRenderer.Dispose();
        petWindow.Dispose();
    }

    private static string ResolvePetSpritesheetPath(string petAssetDir, string petManifestPath)
    {
        if (File.Exists(petManifestPath))
        {
            try
            {
                var json = File.ReadAllText(petManifestPath);
                var manifest = JsonSerializer.Deserialize(json, AppJsonContext.Default.PetManifest);
                if (manifest != null && !string.IsNullOrWhiteSpace(manifest.SpritesheetPath))
                {
                    return Path.IsPathRooted(manifest.SpritesheetPath)
                        ? manifest.SpritesheetPath
                        : Path.Combine(petAssetDir, manifest.SpritesheetPath);
                }
            }
            catch
            {
                // ponytail: [fallback pet manifest] -> [surface config diagnostics]
            }
        }

        return Path.Combine(petAssetDir, "spritesheet.webp");
    }

    private sealed class ActionNotificationService : INotificationService
    {
        private readonly Action<string, string, int> _notifyAction;

        public ActionNotificationService(Action<string, string, int> notifyAction)
        {
            _notifyAction = notifyAction;
        }

        public void Notify(string text, string mood = "happy", int durationMs = 4000)
        {
            _notifyAction(text, mood, durationMs);
        }
    }
}
