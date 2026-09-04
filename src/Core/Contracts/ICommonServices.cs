using DanlangA_Bot.Core.Models;

namespace DanlangA_Bot.Core.Contracts;

public interface IConfigManager
{
    AppConfig CurrentConfig { get; }
    void Load(string configPath);
    void Save(string configPath);
    string ResolvePath(string inputPath);
}

public interface IIpcServer : IAsyncDisposable
{
    Task StartAsync(string pipeName, CancellationToken cancellationToken);
    event Action<IpcMessage>? OnMessageReceived;
}

public interface INotificationService
{
    void Notify(string text, string mood = "happy", int durationMs = 4000);
}
