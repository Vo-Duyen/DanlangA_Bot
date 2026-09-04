using DanlangA_Bot.Core.Models;

namespace DanlangA_Bot.Platform;

public sealed class OcclusionDetector : IDisposable
{
    private readonly OcclusionConfig _config;
    private readonly nint _ownerHwnd;
    private Timer? _checkTimer;
    private bool _isOccluded;
    private bool _disposed;

    public event Action<bool>? OnOcclusionChanged;

    public OcclusionDetector(OcclusionConfig config, nint ownerHwnd)
    {
        _config = config;
        _ownerHwnd = ownerHwnd;
    }

    public void Start()
    {
        if (!_config.FullscreenSnooze) return;

        int interval = Math.Max(500, _config.CheckIntervalMs);
        _checkTimer = new Timer(_ => CheckOcclusion(), null, interval, interval);
    }

    private void CheckOcclusion()
    {
        if (_disposed) return;

        nint fg = Win32Interop.GetForegroundWindow();
        if (fg == 0 || fg == _ownerHwnd || fg == Win32Interop.GetDesktopWindow())
        {
            UpdateOcclusionState(false);
            return;
        }

        if (Win32Interop.GetWindowRect(fg, out var rect))
        {
            int screenW = Win32Interop.GetSystemMetrics(Win32Interop.SM_CXSCREEN);
            int screenH = Win32Interop.GetSystemMetrics(Win32Interop.SM_CYSCREEN);

            // If foreground window covers the screen completely (Fullscreen Game or Media Player)
            bool coversScreen = rect.Left <= 0 && rect.Top <= 0 && rect.Right >= screenW && rect.Bottom >= screenH;
            UpdateOcclusionState(coversScreen);
        }
    }

    private void UpdateOcclusionState(bool occluded)
    {
        if (_isOccluded != occluded)
        {
            _isOccluded = occluded;
            OnOcclusionChanged?.Invoke(_isOccluded);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _checkTimer?.Dispose();
        }
    }
}
