using SkiaSharp;

namespace DanlangA_Bot.Rendering;

/// <summary>
/// Loads a spritesheet.webp (grid atlas) and provides 32-bit BGRA pixel data
/// per animation frame, sliced from the atlas using row/column indices.
/// </summary>
public sealed class SpritesheetAtlas : IDisposable
{
    // Rem spritesheet constants: 1536x1872, 192x208 per cell, 8 cols x 9 rows
    private const int AtlasCols = 8;

    private SKBitmap? _atlas;
    private int _cellW;
    private int _cellH;
    private bool _disposed;

    /// <summary>Whether a valid spritesheet has been loaded.</summary>
    public bool IsLoaded => _atlas != null;
    public int CellWidth => _cellW;
    public int CellHeight => _cellH;

    /// <summary>
    /// Load a WebP sprite sheet atlas from disk.
    /// cellW/cellH define single frame dimensions in pixels.
    /// </summary>
    public bool Load(string webpPath, int cellW, int cellH)
    {
        if (!File.Exists(webpPath)) return false;

        try
        {
            _atlas?.Dispose();
            using var data = SKData.Create(webpPath);
            var bmp = SKBitmap.Decode(data);
            if (bmp == null) return false;

            // Ensure BGRA8888 format for direct Win32 DIB compatibility
            if (bmp.ColorType != SKColorType.Bgra8888)
            {
                var converted = new SKBitmap(bmp.Width, bmp.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var canvas = new SKCanvas(converted);
                canvas.DrawBitmap(bmp, 0, 0);
                bmp.Dispose();
                _atlas = converted;
            }
            else
            {
                _atlas = bmp;
            }

            _cellW = cellW;
            _cellH = cellH;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extract a single frame from the atlas and scale it to targetW x targetH.
    /// Returns a newly allocated BGRA uint[] array ready to blit into a DIB buffer.
    /// The result should be cached at the caller level; do not call per-frame without caching.
    /// </summary>
    public uint[]? GetFrameScaled(int row, int col, int targetW, int targetH)
    {
        if (_atlas == null) return null;
        if (col < 0 || col >= AtlasCols) return null;

        int srcX = col * _cellW;
        int srcY = row * _cellH;

        if (srcX + _cellW > _atlas.Width || srcY + _cellH > _atlas.Height) return null;

        // Crop cell from atlas
        using var cell = new SKBitmap(_cellW, _cellH, SKColorType.Bgra8888, SKAlphaType.Premul);
        using (var canvas = new SKCanvas(cell))
        {
            var srcRect = new SKRectI(srcX, srcY, srcX + _cellW, srcY + _cellH);
            var dstRect = new SKRect(0, 0, _cellW, _cellH);
            canvas.DrawBitmap(_atlas, srcRect, dstRect, new SKPaint
            {
                FilterQuality = SKFilterQuality.High
            });
        }

        // Scale to target size
        using var scaled = cell.Resize(new SKImageInfo(targetW, targetH, SKColorType.Bgra8888, SKAlphaType.Premul), SKFilterQuality.High);
        if (scaled == null) return null;

        // Copy pixel data into managed uint[] for DIB blit
        int pixelCount = targetW * targetH;
        var result = new uint[pixelCount];
        unsafe
        {
            uint* src = (uint*)scaled.GetPixels().ToPointer();
            for (int i = 0; i < pixelCount; i++)
            {
                result[i] = src[i];
            }
        }

        return result;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _atlas?.Dispose();
            _atlas = null;
        }
    }
}
