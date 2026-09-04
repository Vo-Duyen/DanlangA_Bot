using System.Runtime.InteropServices;
using DanlangA_Bot.Rendering;

namespace DanlangA_Bot.Platform;

public sealed class SpeechBubbleWindow : IDisposable
{
    private const string WindowClassName = "DanlangSpeechBubbleClass";
    private nint _hWnd;
    private nint _parentHwnd;
    private DibBuffer? _buffer;
    private Timer? _dismissTimer;
    private Win32Interop.WndProcDelegate? _wndProc;
    private bool _disposed;

    public nint Handle => _hWnd;

    public void Initialize(nint parentHwnd)
    {
        _parentHwnd = parentHwnd;
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

        _hWnd = Win32Interop.CreateWindowExW(
            Win32Interop.WS_EX_LAYERED | Win32Interop.WS_EX_TOPMOST | Win32Interop.WS_EX_TOOLWINDOW | Win32Interop.WS_EX_NOACTIVATE,
            WindowClassName,
            "DanlangSpeechBubble",
            Win32Interop.WS_POPUP,
            -1000, -1000, 200, 80,
            _parentHwnd,
            0,
            Win32Interop.GetModuleHandleW(null),
            0);
    }

    public void ShowBubble(string text, string mood, int petX, int petY, int petW, int petH, int durationMs = 4000)
    {
        if (_hWnd == 0) return;

        // Auto-size based on text length
        int width = Math.Clamp(text.Length * 9 + 40, 140, 300);
        int height = Math.Clamp((text.Length / 22 + 1) * 22 + 34, 50, 120);

        _buffer?.Dispose();
        _buffer = new DibBuffer(width, height);

        DrawBubbleSurface(text, mood, width, height);

        // Position above pet
        int posX = petX + (petW / 2) - (width / 2);
        int posY = petY - height - 8;

        // Prevent off-screen left/top
        if (posX < 10) posX = 10;
        int screenW = Win32Interop.GetSystemMetrics(Win32Interop.SM_CXSCREEN);
        if (posX + width > screenW - 10) posX = screenW - width - 10;
        if (posY < 10) posY = petY + petH + 8; // flip below if off top

        var pptDst = new Win32Interop.POINT(posX, posY);
        var psize = new Win32Interop.SIZE(width, height);
        var pptSrc = new Win32Interop.POINT(0, 0);
        var blend = new Win32Interop.BLENDFUNCTION
        {
            BlendOp = Win32Interop.AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = 250,
            AlphaFormat = Win32Interop.AC_SRC_ALPHA
        };

        Win32Interop.UpdateLayeredWindow(
            _hWnd,
            0,
            ref pptDst,
            ref psize,
            _buffer.MemoryDc,
            ref pptSrc,
            0,
            ref blend,
            Win32Interop.ULW_ALPHA);

        Win32Interop.ShowWindow(_hWnd, Win32Interop.SW_SHOWNOACTIVATE);

        _dismissTimer?.Dispose();
        _dismissTimer = new Timer(_ => HideBubble(), null, durationMs, Timeout.Infinite);
    }

    public void HideBubble()
    {
        if (_hWnd != 0)
        {
            Win32Interop.ShowWindow(_hWnd, Win32Interop.SW_HIDE);
        }
    }

    private unsafe void DrawBubbleSurface(string text, string mood, int width, int height)
    {
        if (_buffer == null || _buffer.BitsPtr == 0) return;

        uint* dst = (uint*)_buffer.BitsPtr;
        _buffer.Clear();

        // Modern Sleek Bubble colors (Dark Glass: RGBA 30, 30, 46, 235)
        const byte bgA = 230;
        const byte bgR = 24;
        const byte bgG = 24;
        const byte bgB = 37;
        uint bgPremult = ((uint)bgA << 24) |
                         ((uint)((bgR * bgA) / 255) << 16) |
                         ((uint)((bgG * bgA) / 255) << 8) |
                         ((uint)((bgB * bgA) / 255));

        // Border color (Soft Cyan/Lavender outline)
        const byte bA = 255;
        const byte bR = 137;
        const byte bG = 180;
        const byte bB = 250;
        uint borderPremult = ((uint)bA << 24) | ((uint)bR << 16) | ((uint)bG << 8) | bB;

        int cornerR = 8;
        int bubbleBottom = height - 10;

        for (int y = 0; y < bubbleBottom; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isCorner = (x < cornerR && y < cornerR && (cornerR - x) * (cornerR - x) + (cornerR - y) * (cornerR - y) > cornerR * cornerR) ||
                                (x >= width - cornerR && y < cornerR && (x - (width - cornerR)) * (x - (width - cornerR)) + (cornerR - y) * (cornerR - y) > cornerR * cornerR) ||
                                (x < cornerR && y >= bubbleBottom - cornerR && (cornerR - x) * (cornerR - x) + (y - (bubbleBottom - cornerR)) * (y - (bubbleBottom - cornerR)) > cornerR * cornerR) ||
                                (x >= width - cornerR && y >= bubbleBottom - cornerR && (x - (width - cornerR)) * (x - (width - cornerR)) + (y - (bubbleBottom - cornerR)) * (y - (bubbleBottom - cornerR)) > cornerR * cornerR);

                if (isCorner) continue;

                bool isBorder = x == 0 || x == width - 1 || y == 0 || y == bubbleBottom - 1;
                dst[y * width + x] = isBorder ? borderPremult : bgPremult;
            }
        }

        // Pointer Tail pointing downwards
        int tailCx = width / 2;
        for (int ty = 0; ty < 8; ty++)
        {
            int y = bubbleBottom + ty;
            int halfW = 7 - ty;
            for (int tx = -halfW; tx <= halfW; tx++)
            {
                int x = tailCx + tx;
                if (x >= 0 && x < width && y < height)
                {
                    bool isBorder = tx == -halfW || tx == halfW || ty == 7;
                    dst[y * width + x] = isBorder ? borderPremult : bgPremult;
                }
            }
        }

        // Render text using Win32 GDI onto memory DC
        nint hdc = _buffer.MemoryDc;
        SetTextColor(hdc, 0x00FFFFFF); // White text (0x00BBGGRR)
        SetBkMode(hdc, 1);             // TRANSPARENT

        var rect = new Win32Interop.RECT
        {
            Left = 12,
            Top = 8,
            Right = width - 12,
            Bottom = bubbleBottom - 6
        };

        DrawTextW(hdc, text, text.Length, ref rect, 0x00000000 | 0x00000010 | 0x00000001); // DT_TOP | DT_WORDBREAK | DT_CENTER

        // Fix alpha channel for GDI text glyphs (GDI draws text with alpha 0)
        FixGdiTextAlpha(dst, width, height);
    }

    private unsafe void FixGdiTextAlpha(uint* dst, int width, int height)
    {
        for (int i = 0; i < width * height; i++)
        {
            uint pixel = dst[i];
            byte a = (byte)((pixel >> 24) & 0xFF);
            if (a == 0 && (pixel & 0x00FFFFFF) != 0)
            {
                // Non-zero RGB from text, force full alpha and premultiply
                dst[i] = 0xFF000000 | pixel;
            }
        }
    }

    private nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == Win32Interop.WM_LBUTTONDOWN)
        {
            HideBubble();
            return 0;
        }
        return Win32Interop.DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    [DllImport("gdi32.dll")]
    private static extern uint SetTextColor(nint hdc, uint crColor);

    [DllImport("gdi32.dll")]
    private static extern int SetBkMode(nint hdc, int iBkMode);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int DrawTextW(nint hDC, string lpchText, int nCount, ref Win32Interop.RECT lpRect, uint uFormat);

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _dismissTimer?.Dispose();
            _buffer?.Dispose();
            if (_hWnd != 0)
            {
                Win32Interop.DestroyWindow(_hWnd);
                _hWnd = 0;
            }
        }
    }
}
