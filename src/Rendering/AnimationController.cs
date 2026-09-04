using DanlangA_Bot.Core.Contracts;
using DanlangA_Bot.Platform;

namespace DanlangA_Bot.Rendering;

/// <summary>
/// Điều phối vòng lặp animation với 0.0% CPU Idle.
/// Khi pet ở trạng thái tĩnh (fps=0 hoặc bị occlusion), timer ngừng hoàn toàn.
/// </summary>
public sealed class AnimationController : IDisposable
{
    private readonly IPetWindow _window;
    private readonly IFsmEngine _fsm;
    private readonly SpriteRenderer _renderer;
    private readonly DibBuffer _dibBuffer;

    private Timer? _activeAnimationTimer;
    private Timer? _stateTransitionTimer;
    private bool _isFrozen;
    private bool _disposed;

    public AnimationController(IPetWindow window, IFsmEngine fsm, SpriteRenderer renderer)
    {
        _window = window;
        _fsm = fsm;
        _renderer = renderer;

        int initialSize = _renderer.GetRenderSize(_window.CurrentScale);
        _dibBuffer = new DibBuffer(initialSize, initialSize);

        _fsm.OnFrameChanged += HandleFrameChanged;
    }

    public void Start()
    {
        RenderCurrentFrame();
        ScheduleNextAnimationTick();
    }

    public void SetFrozen(bool frozen)
    {
        if (_isFrozen == frozen) return;
        _isFrozen = frozen;

        if (_isFrozen)
        {
            StopAnimationTimer();
        }
        else
        {
            ScheduleNextAnimationTick();
        }
    }

    public void OnScaleChanged()
    {
        int newSize = _renderer.GetRenderSize(_window.CurrentScale);
        _dibBuffer.Resize(newSize, newSize);
        RenderCurrentFrame();
    }

    private void HandleFrameChanged(string state, int frameIndex, int atlasRow, int fps)
    {
        if (_isFrozen) return;

        RenderCurrentFrame();

        if (fps <= 0)
        {
            StopAnimationTimer();
            ScheduleIdleWakeupTimer();
        }
        else
        {
            StartAnimationTimer(fps);
        }
    }

    private void RenderCurrentFrame()
    {
        if (_disposed) return;

        _renderer.RenderFrame(
            _dibBuffer,
            _fsm.CurrentStateName,
            _fsm.CurrentAtlasRow,
            _fsm.CurrentFrameIndex,
            _window.CurrentScale);

        _window.UpdateSurface(_dibBuffer.MemoryDc, _dibBuffer.Width, _dibBuffer.Height, 255);
    }

    private void StartAnimationTimer(int fps)
    {
        int intervalMs = Math.Clamp(1000 / Math.Max(1, fps), 16, 500);

        if (_activeAnimationTimer == null)
        {
            _activeAnimationTimer = new Timer(
                _ => OnAnimationTick(intervalMs / 1000.0),
                null, intervalMs, intervalMs);
        }
        else
        {
            _activeAnimationTimer.Change(intervalMs, intervalMs);
        }
    }

    private void StopAnimationTimer()
    {
        _activeAnimationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void ScheduleIdleWakeupTimer()
    {
        _stateTransitionTimer?.Dispose();
        _stateTransitionTimer = new Timer(_ =>
        {
            if (!_isFrozen && !_disposed)
                _fsm.Update(1.0);
        }, null, 5000, 5000);
    }

    private void OnAnimationTick(double deltaSeconds)
    {
        if (_isFrozen || _disposed) return;
        _fsm.Update(deltaSeconds);
    }

    private void ScheduleNextAnimationTick()
    {
        if (_fsm.CurrentFps > 0)
            StartAnimationTimer(_fsm.CurrentFps);
        else
        {
            StopAnimationTimer();
            ScheduleIdleWakeupTimer();
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _fsm.OnFrameChanged -= HandleFrameChanged;
            _activeAnimationTimer?.Dispose();
            _stateTransitionTimer?.Dispose();
            _dibBuffer.Dispose();
        }
    }
}
