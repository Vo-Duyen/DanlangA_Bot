using System;
using System.IO;
using SkiaSharp;

// Analyze Rem spritesheet atlas: 1536x1872, cells 192x208
// and count frames from each animated preview WebP

class AtlasAnalyzer
{
    static void Main(string[] args)
    {
        string baseDir = args.Length > 0 ? args[0] : @"C:\Users\Admin\Desktop\DanlangA_Bot\assets\rem--l1";
        string spritesheetPath = Path.Combine(baseDir, "spritesheet.webp");
        string previewsDir = Path.Combine(baseDir, "previews");

        Console.WriteLine("=== Spritesheet Atlas Info ===");
        if (File.Exists(spritesheetPath))
        {
            using var data = SKData.Create(spritesheetPath);
            using var bmp = SKBitmap.Decode(data);
            if (bmp != null)
            {
                Console.WriteLine($"Atlas dimensions: {bmp.Width} x {bmp.Height}");
                int cellW = 192, cellH = 208;
                int cols = bmp.Width / cellW;
                int rows = bmp.Height / cellH;
                Console.WriteLine($"Cell size: {cellW} x {cellH}");
                Console.WriteLine($"Grid: {cols} cols x {rows} rows");
            }
        }

        Console.WriteLine("\n=== Preview Animation Frame Counts ===");
        string[] states = { "idle", "waving", "waiting", "running", "running-right", "running-left", "jumping", "review", "failed" };
        for (int i = 0; i < states.Length; i++)
        {
            string state = states[i];
            string path = Path.Combine(previewsDir, $"{state}.webp");
            if (!File.Exists(path)) { Console.WriteLine($"Row {i} [{state}]: file not found"); continue; }

            using var fileData = SKData.Create(path);
            using var codec = SKCodec.Create(fileData);
            if (codec == null) { Console.WriteLine($"Row {i} [{state}]: cannot decode"); continue; }

            int frameCount = codec.FrameCount;
            int duration = 0;
            if (frameCount > 0)
            {
                var frameInfo = codec.FrameInfo;
                if (frameInfo != null && frameInfo.Length > 0)
                    duration = frameInfo[0].Duration;
            }
            int fps = duration > 0 ? (int)Math.Round(1000.0 / duration) : 0;
            Console.WriteLine($"Row {i} [{state}]: {frameCount} frames, {duration}ms/frame, ~{fps} FPS");
        }
    }
}
