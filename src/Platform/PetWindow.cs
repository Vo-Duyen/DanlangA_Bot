using System.Runtime.InteropServices;
using DanlangA_Bot.Core.Contracts;
using DanlangA_Bot.Core.Models;

namespace DanlangA_Bot.Platform;

public sealed class PetWindow : IPetWindow, IDisposable
{
    private const string WindowClassName = "DanlangPetWindowClass";
    private readonly AppConfig _config;
    private readonly IFsmEngine _fsm;

    private nint _hWnd;
    private Win32Interop.WndProcDelegate? _wndProc;
    private SpeechBubbleWindow? _speechBubble;
    private int _currentX;
    private int _currentY;
    private int _currentWidth;
    private int _currentHeight;
    private double _scale = 1.0;
    private bool _isClickThrough;
    private bool _disposed;

    public nint Handle => _hWnd;
    public double CurrentScale => _scale;
    public bool IsClickThrough => _isClickThrough;

    public event Action? OnScaleChanged;

    public PetWindow(AppConfig config, IFsmEngine fsm)
    {
        _config = config;
        _fsm = fsm;
        _scale = Math.Clamp(_config.Window.Scale, _config.Window.MinScale, _config.Window.MaxScale);
        _isClickThrough = _config.Window.ClickThrough;
    }

    public void Initialize()
    {
        _wndProc = WndProc;

        var wndClass = new Win32Interop.WNDCLASSEX
        {
            cbSize = (uint)Marshal.SizeOf<Win32Interop.WNDCLASSEX>(),
            style = 0,
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = Win32Interop.GetModuleHandleW(null),
            hCursor = Win32Interop.LoadCursorW(0, (nint)32512), // IDC_ARROW
            lpszClassName = WindowClassName
        };

        Win32Interop.RegisterClassExW(ref wndClass);

        int exStyle = Win32Interop.WS_EX_LAYERED | Win32Interop.WS_EX_TOPMOST | Win32Interop.WS_EX_TOOLWINDOW;
        if (_isClickThrough)
        {
            exStyle |= Win32Interop.WS_EX_TRANSPARENT;
        }

        int screenW = Win32Interop.GetSystemMetrics(Win32Interop.SM_CXSCREEN);
        int screenH = Win32Interop.GetSystemMetrics(Win32Interop.SM_CYSCREEN);

        _currentWidth = (int)(32 * _scale * 3);
        _currentHeight = (int)(32 * _scale * 3);

        _currentX = (int)((_config.Window.RelativeXPercent / 100.0) * screenW) - (_currentWidth / 2);
        _currentY = (int)((_config.Window.RelativeYPercent / 100.0) * screenH) - (_currentHeight / 2);

        _hWnd = Win32Interop.CreateWindowExW(
            exStyle,
            WindowClassName,
            "DanlangDesktopPet",
            Win32Interop.WS_POPUP | Win32Interop.WS_VISIBLE,
            _currentX,
            _currentY,
            _currentWidth,
            _currentHeight,
            0,
            0,
            Win32Interop.GetModuleHandleW(null),
            0);

        _speechBubble = new SpeechBubbleWindow();
        _speechBubble.Initialize(_hWnd);

        Win32Interop.ShowWindow(_hWnd, Win32Interop.SW_SHOW);
        Win32Interop.UpdateWindow(_hWnd);
    }

    public void SetPositionPercent(double xPercent, double yPercent)
    {
        int screenW = Win32Interop.GetSystemMetrics(Win32Interop.SM_CXSCREEN);
        int screenH = Win32Interop.GetSystemMetrics(Win32Interop.SM_CYSCREEN);

        _currentX = (int)((xPercent / 100.0) * screenW) - (_currentWidth / 2);
        _currentY = (int)((yPercent / 100.0) * screenH) - (_currentHeight / 2);

        Win32Interop.SetWindowPos(_hWnd, Win32Interop.HWND_TOPMOST, _currentX, _currentY, 0, 0,
            Win32Interop.SWP_NOSIZE | Win32Interop.SWP_NOACTIVATE);
    }

    public void SetScale(double scale)
    {
        double newScale = Math.Clamp(scale, _config.Window.MinScale, _config.Window.MaxScale);
        if (Math.Abs(_scale - newScale) < 0.01) return;

        _scale = newScale;
        _config.Window.Scale = _scale;

        _currentWidth = (int)(32 * _scale * 3);
        _currentHeight = (int)(32 * _scale * 3);

        OnScaleChanged?.Invoke();
    }

    public void SetClickThrough(bool enabled)
    {
        _isClickThrough = enabled;
        _config.Window.ClickThrough = enabled;

        int exStyle = Win32Interop.GetWindowLongW(_hWnd, Win32Interop.GWL_EXSTYLE);
        if (enabled)
        {
            exStyle |= Win32Interop.WS_EX_TRANSPARENT;
        }
        else
        {
            exStyle &= ~Win32Interop.WS_EX_TRANSPARENT;
        }
        Win32Interop.SetWindowLongW(_hWnd, Win32Interop.GWL_EXSTYLE, exStyle);
    }

    public void ShowNotification(string text, string mood, int durationMs)
    {
        _fsm.TriggerState("happy", force: true);
        UpdateCurrentPositionFromWindow();
        _speechBubble?.ShowBubble(text, mood, _currentX, _currentY, _currentWidth, _currentHeight, durationMs);
    }

    public void UpdateSurface(nint hdcSrc, int width, int height, byte alpha = 255)
    {
        if (_hWnd == 0 || hdcSrc == 0) return;

        _currentWidth = width;
        _currentHeight = height;

        UpdateCurrentPositionFromWindow();

        var pptDst = new Win32Interop.POINT(_currentX, _currentY);
        var psize = new Win32Interop.SIZE(width, height);
        var pptSrc = new Win32Interop.POINT(0, 0);
        var blend = new Win32Interop.BLENDFUNCTION
        {
            BlendOp = Win32Interop.AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = alpha,
            AlphaFormat = Win32Interop.AC_SRC_ALPHA
        };

        Win32Interop.UpdateLayeredWindow(
            _hWnd,
            0,
            ref pptDst,
            ref psize,
            hdcSrc,
            ref pptSrc,
            0,
            ref blend,
            Win32Interop.ULW_ALPHA);
    }

    public void RunMessageLoop()
    {
        // Zero CPU Idle: GetMessageW blocks until a message arrives, zero polling!
        while (Win32Interop.GetMessageW(out var msg, 0, 0, 0))
        {
            Win32Interop.TranslateMessage(ref msg);
            Win32Interop.DispatchMessageW(ref msg);
        }
    }

    public void Close()
    {
        if (_hWnd != 0)
        {
            Win32Interop.DestroyWindow(_hWnd);
            _hWnd = 0;
        }
    }

    private void UpdateCurrentPositionFromWindow()
    {
        if (Win32Interop.GetWindowRect(_hWnd, out var rect))
        {
            _currentX = rect.Left;
            _currentY = rect.Top;
        }
    }

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        switch (msg)
        {
            case Win32Interop.WM_LBUTTONDOWN:
                _fsm.TriggerState("dragging", force: true);
                Win32Interop.ReleaseCapture();
                Win32Interop.SendMessageW(hWnd, Win32Interop.WM_NCLBUTTONDOWN, (nint)Win32Interop.HTCAPTION, 0);
                _fsm.TriggerState("idle", force: true);
                UpdateRelativePercentageCoords();
                return 0;

            case Win32Interop.WM_MOUSEWHEEL:
                short delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
                double scaleDelta = delta > 0 ? 0.15 : -0.15;
                SetScale(_scale + scaleDelta);
                return 0;

            case Win32Interop.WM_RBUTTONUP:
                // Quick Toggle Click-Through or Action Menu
                ShowNotification("Click-through: Nhấn đúp để tương tác xuyên thấu!", "happy", 3000);
                return 0;

            case Win32Interop.WM_DESTROY:
                Win32Interop.PostQuitMessage(0);
                return 0;
        }

        return Win32Interop.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private void UpdateRelativePercentageCoords()
    {
        UpdateCurrentPositionFromWindow();
        int screenW = Win32Interop.GetSystemMetrics(Win32Interop.SM_CXSCREEN);
        int screenH = Win32Interop.GetSystemMetrics(Win32Interop.SM_CYSCREEN);

        if (screenW > 0 && screenH > 0)
        {
            _config.Window.RelativeXPercent = ((_currentX + (_currentWidth / 2.0)) / screenW) * 100.0;
            _config.Window.RelativeYPercent = ((_currentY + (_currentHeight / 2.0)) / screenH) * 100.0;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _speechBubble?.Dispose();
            Close();
        }
    }
}
