using Microsoft.Win32;

namespace DanlangA_Bot.Platform;

public static class AutostartManager
{
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static void EnsureSelfHealingAutostart(string appName, bool enabled)
    {
        string? currentExePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExePath)) return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: true);
            if (key == null) return;

            if (!enabled)
            {
                if (key.GetValue(appName) != null)
                {
                    key.DeleteValue(appName, false);
                }
                return;
            }

            string expectedValue = $"\"{currentExePath}\"";
            object? existing = key.GetValue(appName);

            // Self-healing: if not set or path has changed (e.g. folder moved), heal immediately!
            if (existing == null || !string.Equals(existing.ToString(), expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                key.SetValue(appName, expectedValue);
            }
        }
        catch
        {
            // Non-admin context: silently ignore if restricted
        }
    }
}
