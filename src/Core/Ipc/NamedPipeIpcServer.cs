using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using DanlangA_Bot.Core.Contracts;
using DanlangA_Bot.Core.Models;

namespace DanlangA_Bot.Core.Ipc;

public sealed class NamedPipeIpcServer : IIpcServer
{
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    public event Action<IpcMessage>? OnMessageReceived;

    public Task StartAsync(string pipeName, CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _serverTask = Task.Run(() => ServerLoopAsync(pipeName, _cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    private async Task ServerLoopAsync(string pipeName, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            NamedPipeServerStream? server = null;
            try
            {
                server = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);

                await server.WaitForConnectionAsync(ct);

                using (server)
                using (var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true))
                using (var writer = new StreamWriter(server, Encoding.UTF8, leaveOpen: true) { AutoFlush = true })
                {
                    string? line = await reader.ReadLineAsync(ct);
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var msg = JsonSerializer.Deserialize(line, AppJsonContext.Default.IpcMessage);
                        if (msg != null)
                        {
                            await writer.WriteLineAsync("{\"status\":\"ok\"}");
                            OnMessageReceived?.Invoke(msg);
                        }
                        else
                        {
                            await writer.WriteLineAsync("{\"error\":\"invalid_payload\"}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Delay slightly on unexpected pipe errors to prevent spin loop
                try
                {
                    await Task.Delay(500, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            if (_serverTask != null)
            {
                try { await _serverTask; } catch { }
            }
            _cts.Dispose();
            _cts = null;
        }
    }
}
