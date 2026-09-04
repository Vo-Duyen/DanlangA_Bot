using System.Text.Json;
using System.Text.RegularExpressions;
using DanlangA_Bot.Core.Contracts;
using DanlangA_Bot.Core.Models;

namespace DanlangA_Bot.Core.Config;

public sealed class ConfigManager : IConfigManager
{
    private AppConfig _config = new();
    public AppConfig CurrentConfig => _config;

    public void Load(string configPath)
    {
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var loaded = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig);
                if (loaded != null)
                {
                    _config = loaded;
                    return;
                }
            }
            catch
            {
                // ponytail: [silent config error] -> [report diagnostics]
            }
        }

        _config = new AppConfig();
    }

    public void Save(string configPath)
    {
        try
        {
            string? dir = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(_config, AppJsonContext.Default.AppConfig);
            File.WriteAllText(configPath, json);
        }
        catch
        {
            // Ignore write errors to guarantee non-fatal operation
        }
    }

    public string ResolvePath(string inputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath)) return string.Empty;

        // Custom URI protocols (e.g. vscode://, spotify:, steam://) pass through unchanged
        if (inputPath.Contains("://") || inputPath.StartsWith("mailto:") || inputPath.StartsWith("spotify:"))
        {
            return inputPath;
        }

        // Expand environment variables like %APPDATA%, %PROGRAMFILES%
        string expanded = Environment.ExpandEnvironmentVariables(inputPath);

        // If relative path, resolve against base directory
        if (!Path.IsPathRooted(expanded))
        {
            expanded = Path.Combine(AppContext.BaseDirectory, expanded);
        }

        return expanded;
    }
}
