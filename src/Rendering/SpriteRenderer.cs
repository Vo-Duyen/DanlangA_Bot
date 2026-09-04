using SkiaSharp;

namespace DanlangA_Bot.Rendering;

/// <summary>
/// Hybrid sprite renderer: dùng SpritesheetAtlas (WebP Rem) nếu có,
/// fallback về procedural pixel mascot nếu không tìm thấy file.
/// Cache mỗi frame đã scale để zero allocation trong hot path.
/// </summary>
public sealed class SpriteRenderer : IDisposable
{
    // Rem atlas specs: 192x208 per cell, 8 cols x 9 rows
    public const int AtlasCellW = 192;
    public const int AtlasCellH = 208;
    private const int AtlasCols = 8;

    // Procedural fallback specs
    public const int ProceduralBase = 32;

    private SpritesheetAtlas? _atlas;
    private bool _usingAtlas;

    // Frame cache: key = (row, col, targetSize)
    private readonly Dictionary<long, uint[]> _frameCache = [];

    // Procedural fallback sprites
    private readonly Dictionary<string, List<uint[]>> _proceduralCache = [];

    private bool _disposed;

    public bool IsUsingRealSprite => _usingAtlas;

    public SpriteRenderer(string? spritesheetPath = null)
    {
        if (!string.IsNullOrEmpty(spritesheetPath) && File.Exists(spritesheetPath))
        {
            var atlas = new SpritesheetAtlas();
            if (atlas.Load(spritesheetPath, AtlasCellW, AtlasCellH))
            {
                _atlas = atlas;
                _usingAtlas = true;
            }
            else
            {
                atlas.Dispose();
            }
        }

        if (!_usingAtlas)
        {
            GenerateProceduralFallback();
        }
    }

    public int GetRenderSize(double scale)
    {
        if (_usingAtlas)
        {
            // Render Rem at a reasonable desktop size: 192px * scale * display factor
            return (int)(AtlasCellW * Math.Max(0.5, scale));
        }
        return (int)(ProceduralBase * Math.Max(0.5, scale) * 3);
    }

    public void RenderFrame(DibBuffer target, string stateName, int atlasRow, int frameIndex, double scale)
    {
        if (_usingAtlas && _atlas != null)
        {
            RenderAtlasFrame(target, atlasRow, frameIndex, scale);
        }
        else
        {
            RenderProceduralFrame(target, stateName, frameIndex);
        }
    }

    private void RenderAtlasFrame(DibBuffer target, int atlasRow, int frameIndex, double scale)
    {
        int targetSize = GetRenderSize(scale);

        // Resize buffer if needed
        target.Resize(targetSize, (int)(AtlasCellH * Math.Max(0.5, scale)));

        long cacheKey = ((long)atlasRow << 20) | ((long)frameIndex << 12) | ((long)targetSize & 0xFFF);
        if (!_frameCache.TryGetValue(cacheKey, out var pixels))
        {
            int targetH = (int)(AtlasCellH * Math.Max(0.5, scale));
            pixels = _atlas!.GetFrameScaled(atlasRow, frameIndex, targetSize, targetH) ?? [];
            _frameCache[cacheKey] = pixels;
        }

        if (pixels.Length > 0)
        {
            int w = target.Width;
            int h = target.Height;
            target.BlitDirect(pixels, w, h);
        }
    }

    private void RenderProceduralFrame(DibBuffer target, string stateName, int frameIndex)
    {
        int baseW = ProceduralBase;
        int baseH = ProceduralBase;
        target.Resize(target.Width, target.Height);

        if (_proceduralCache.TryGetValue(stateName, out var frames) && frames.Count > 0)
        {
            int idx = Math.Clamp(frameIndex, 0, frames.Count - 1);
            target.BlitScaled(frames[idx], baseW, baseH);
        }
        else if (_proceduralCache.TryGetValue("idle", out var idle) && idle.Count > 0)
        {
            target.BlitScaled(idle[0], baseW, baseH);
        }
    }

    private void GenerateProceduralFallback()
    {
        _proceduralCache["idle"] = [CreateMascotFrame(0, 0, 0, 0)];
        _proceduralCache["waving"] = [
            CreateMascotFrame(0, 0, 1, 0),
            CreateMascotFrame(0, 0, -1, 0),
            CreateMascotFrame(0, 0, 2, -1),
            CreateMascotFrame(0, 0, 0, 0)
        ];
        _proceduralCache["waiting"] = [
            CreateMascotFrame(0, 0, 0, 0),
            CreateMascotFrame(1, 0, -1, 0),
            CreateMascotFrame(0, 0, 1, 0),
            CreateMascotFrame(1, 0, 0, -1),
            CreateMascotFrame(0, 0, -1, 0),
            CreateMascotFrame(0, 0, 0, 0)
        ];
        _proceduralCache["running"] = [
            CreateMascotFrame(0, 0, -1, -2, footShift: -2),
            CreateMascotFrame(0, 0, 0, 0, footShift: 0),
            CreateMascotFrame(0, 0, 1, -2, footShift: 2),
            CreateMascotFrame(0, 0, 0, -1, footShift: 1),
            CreateMascotFrame(0, 0, -1, -2, footShift: -1),
            CreateMascotFrame(0, 0, 0, 0, footShift: 0)
        ];
        _proceduralCache["running-right"] = _proceduralCache["running"];
        _proceduralCache["running-left"] = _proceduralCache["running"];
        _proceduralCache["jumping"] = [
            CreateMascotFrame(3, 1, 0, -3),
            CreateMascotFrame(3, 1, 0, -4),
            CreateMascotFrame(3, 1, 0, -3),
            CreateMascotFrame(0, 0, 0, -2),
            CreateMascotFrame(0, 0, 0, 0)
        ];
        _proceduralCache["review"] = _proceduralCache["waiting"];
        _proceduralCache["failed"] = [
            CreateMascotFrame(2, 1, -2, 2, isHanging: true),
            CreateMascotFrame(2, 1, 2, 1, isHanging: true),
            CreateMascotFrame(2, 1, -1, 2, isHanging: true),
            CreateMascotFrame(2, 1, 1, 1, isHanging: true),
            CreateMascotFrame(2, 1, -2, 2, isHanging: true),
            CreateMascotFrame(2, 1, 2, 1, isHanging: true),
            CreateMascotFrame(2, 1, -1, 2, isHanging: true),
            CreateMascotFrame(2, 1, 0, 0)
        ];
        _proceduralCache["dragging"] = [
            CreateMascotFrame(3, 1, -2, -2, isHanging: true),
            CreateMascotFrame(3, 1, 2, -2, isHanging: true)
        ];
        _proceduralCache["happy"] = [
            CreateMascotFrame(4, 2, 1, -2, hasBlush: true),
            CreateMascotFrame(4, 2, -1, -1, hasBlush: true),
            CreateMascotFrame(4, 2, 1, -2, hasBlush: true),
            CreateMascotFrame(4, 2, 0, 0, hasBlush: true)
        ];
    }

    private static uint[] CreateMascotFrame(
        int eyeState,
        int mouthState,
        int earOffset,
        int bobY,
        int footShift = 0,
        bool isHanging = false,
        bool hasBlush = false)
    {
        const int W = ProceduralBase;
        const int H = ProceduralBase;
        uint[] buffer = new uint[W * H];

        const uint cOutline = 0xFF1E1E2E;
        const uint cBody    = 0xFF89B4FA;
        const uint cShade   = 0xFF74C7EC;
        const uint cBelly   = 0xFFF5E0DC;
        const uint cEarIn   = 0xFFF38BA8;
        const uint cEye     = 0xFF11111B;
        const uint cShine   = 0xFFFFFFFF;
        const uint cCheek   = 0xFFF38BA8;

        void Plot(int x, int y, uint c) { if (x >= 0 && x < W && y >= 0 && y < H) buffer[y * W + x] = c; }
        void Fill(int x1, int y1, int w, int h, uint c) { for (int dy = 0; dy < h; dy++) for (int dx = 0; dx < w; dx++) Plot(x1 + dx, y1 + dy, c); }

        int cx = W / 2, cy = H / 2 + bobY;

        Fill(cx - 7 + earOffset, cy - 13, 4, 7, cBody);
        Fill(cx - 6 + earOffset, cy - 12, 2, 5, cEarIn);
        Fill(cx + 3 - earOffset, cy - 13, 4, 7, cBody);
        Fill(cx + 4 - earOffset, cy - 12, 2, 5, cEarIn);

        Fill(cx - 8, cy - 6, 16, 12, cBody);
        Fill(cx - 9, cy - 2, 18, 4, cBody);
        Fill(cx - 4, cy + 2, 8, 5, cBelly);

        int eyeY = cy - 1;
        switch (eyeState)
        {
            case 0: Fill(cx - 5, eyeY, 2, 3, cEye); Fill(cx + 3, eyeY, 2, 3, cEye); Plot(cx - 5, eyeY, cShine); Plot(cx + 3, eyeY, cShine); break;
            case 1: Fill(cx - 5, eyeY + 1, 2, 2, cEye); Fill(cx + 3, eyeY + 1, 2, 2, cEye); break;
            case 2: Plot(cx - 5, eyeY + 1, cEye); Plot(cx - 4, eyeY, cEye); Plot(cx - 3, eyeY + 1, cEye); Plot(cx + 3, eyeY + 1, cEye); Plot(cx + 4, eyeY, cEye); Plot(cx + 5, eyeY + 1, cEye); break;
            case 3: Fill(cx - 6, eyeY - 1, 3, 4, cEye); Fill(cx + 3, eyeY - 1, 3, 4, cEye); Plot(cx - 5, eyeY, cShine); Plot(cx + 4, eyeY, cShine); break;
            case 4: Plot(cx - 5, eyeY, cEye); Plot(cx - 4, eyeY + 1, cEye); Plot(cx - 5, eyeY + 2, cEye); Plot(cx + 4, eyeY, cEye); Plot(cx + 3, eyeY + 1, cEye); Plot(cx + 4, eyeY + 2, cEye); break;
        }

        int mY = cy + 3;
        switch (mouthState)
        {
            case 0: Plot(cx - 1, mY, cOutline); Plot(cx, mY, cOutline); break;
            case 1: Fill(cx - 1, mY, 2, 2, cOutline); break;
            case 2: Plot(cx - 2, mY, cOutline); Plot(cx - 1, mY + 1, cOutline); Plot(cx, mY + 1, cOutline); Plot(cx + 1, mY, cOutline); break;
        }

        if (hasBlush) { Fill(cx - 7, eyeY + 3, 2, 1, cCheek); Fill(cx + 5, eyeY + 3, 2, 1, cCheek); }

        int feetY = cy + 7;
        if (isHanging) { Fill(cx - 6, feetY, 3, 3, cShade); Fill(cx + 3, feetY, 3, 3, cShade); }
        else { Fill(cx - 6 + footShift, feetY, 4, 2, cShade); Fill(cx + 2 - footShift, feetY, 4, 2, cShade); }

        return buffer;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _atlas?.Dispose();
            _frameCache.Clear();
        }
    }
}
