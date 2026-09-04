using System.Runtime.InteropServices;
using DanlangA_Bot.Platform;

namespace DanlangA_Bot.Rendering;

public sealed class DibBuffer : IDisposable
{
    private nint _hdc;
    private nint _hBitmap;
    private nint _oldBitmap;
    private nint _bitsPtr;
    private bool _disposed;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public nint MemoryDc => _hdc;
    public nint BitsPtr => _bitsPtr;

    public DibBuffer(int width, int height)
    {
        Allocate(width, height);
    }

    public void Resize(int width, int height)
    {
        if (Width == width && Height == height && _bitsPtr != 0) return;
        Free();
        Allocate(width, height);
    }

    private void Allocate(int width, int height)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);

        nint screenDc = Win32Interop.GetDC(0);
        _hdc = Win32Interop.CreateCompatibleDC(screenDc);
        Win32Interop.ReleaseDC(0, screenDc);

        var bmi = new Win32Interop.BITMAPINFOHEADER
        {
            biSize = (uint)Marshal.SizeOf<Win32Interop.BITMAPINFOHEADER>(),
            biWidth = Width,
            biHeight = -Height, // Top-down DIB
            biPlanes = 1,
            biBitCount = 32,
            biCompression = Win32Interop.BI_RGB
        };

        _hBitmap = Win32Interop.CreateDIBSection(_hdc, ref bmi, Win32Interop.DIB_RGB_COLORS, out _bitsPtr, 0, 0);
        _oldBitmap = Win32Interop.SelectObject(_hdc, _hBitmap);
    }

    public unsafe void Clear()
    {
        if (_bitsPtr == 0) return;
        new Span<byte>((void*)_bitsPtr, Width * Height * 4).Clear();
    }

    public unsafe void BlitDirect(uint[] srcPixels, int w, int h)
    {
        if (_bitsPtr == 0 || srcPixels.Length < w * h) return;
        if (Width != w || Height != h) Resize(w, h);

        fixed (uint* src = srcPixels)
        {
            Buffer.MemoryCopy(src, (void*)_bitsPtr, (long)w * h * 4, (long)w * h * 4);
        }
    }

    public unsafe void BlitScaled(uint[] srcPixels, int srcW, int srcH)
    {
        if (_bitsPtr == 0 || srcPixels.Length < srcW * srcH) return;

        Clear();

        uint* dst = (uint*)_bitsPtr;
        int dstW = Width;
        int dstH = Height;

        // High-speed nearest neighbor scaling with alpha premultiplication
        for (int y = 0; y < dstH; y++)
        {
            int srcY = (y * srcH) / dstH;
            if (srcY >= srcH) srcY = srcH - 1;
            int srcRow = srcY * srcW;
            int dstRow = y * dstW;

            for (int x = 0; x < dstW; x++)
            {
                int srcX = (x * srcW) / dstW;
                if (srcX >= srcW) srcX = srcW - 1;

                uint pixel = srcPixels[srcRow + srcX];
                byte a = (byte)((pixel >> 24) & 0xFF);

                if (a == 0)
                {
                    dst[dstRow + x] = 0;
                }
                else if (a == 255)
                {
                    dst[dstRow + x] = pixel;
                }
                else
                {
                    // Premultiply alpha for UpdateLayeredWindow
                    byte r = (byte)((pixel >> 16) & 0xFF);
                    byte g = (byte)((pixel >> 8) & 0xFF);
                    byte b = (byte)(pixel & 0xFF);

                    byte pr = (byte)((r * a) / 255);
                    byte pg = (byte)((g * a) / 255);
                    byte pb = (byte)((b * a) / 255);

                    dst[dstRow + x] = ((uint)a << 24) | ((uint)pr << 16) | ((uint)pg << 8) | pb;
                }
            }
        }
    }

    private void Free()
    {
        if (_hdc != 0)
        {
            if (_oldBitmap != 0)
            {
                Win32Interop.SelectObject(_hdc, _oldBitmap);
                _oldBitmap = 0;
            }
            Win32Interop.DeleteDC(_hdc);
            _hdc = 0;
        }

        if (_hBitmap != 0)
        {
            Win32Interop.DeleteObject(_hBitmap);
            _hBitmap = 0;
        }

        _bitsPtr = 0;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Free();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    ~DibBuffer() => Free();
}
