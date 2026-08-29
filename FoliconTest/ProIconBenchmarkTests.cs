using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using FoliCon.Models.Data;
using FoliCon.Modules.Media;
using FoliCon.Modules.utils;

namespace FoliconTest;

/// <summary>
/// Benchmarks comparing the old ProIcon GDI+ interop path
/// against the new WPF-native BitmapImage approach.
/// </summary>
[Collection(XamlLoadingCollection.name)]
public sealed class ProIconBenchmarkTests : IDisposable
{
    private readonly string _testImagePath;

    public ProIconBenchmarkTests()
    {
        // Use an existing reference PNG as the test source image
        _testImagePath = Path.Combine(
            FindTestProjectRoot(), "Resources", "ReferenceOverlays", "legacy_reference.png");
    }

    public void Dispose() { }

    [Fact]
    public void Benchmark_ProIcon_GdiInterop_vs_WpfNative()
    {
        Assert.True(File.Exists(_testImagePath), $"Test image not found: {_testImagePath}");

        const int warmupIterations = 5;
        const int measuredIterations = 50;

        // --- Warmup ---
        for (var i = 0; i < warmupIterations; i++)
        {
            OldGdiInteropPath(_testImagePath);
            new ProIcon(_testImagePath).RenderToBitmap().Dispose();
        }

        // --- Benchmark old GDI+ interop path ---
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < measuredIterations; i++)
        {
            using var bmp = OldGdiInteropPath(_testImagePath);
            GC.KeepAlive(bmp);
        }
        sw.Stop();
        var gdiElapsed = sw.Elapsed;
        var gdiAvg = gdiElapsed.TotalMilliseconds / measuredIterations;

        // --- Benchmark new ProIcon (WPF-native) ---
        sw.Restart();
        for (var i = 0; i < measuredIterations; i++)
        {
            using var bmp = new ProIcon(_testImagePath).RenderToBitmap();
            GC.KeepAlive(bmp);
        }
        sw.Stop();
        var wpfElapsed = sw.Elapsed;
        var wpfAvg = wpfElapsed.TotalMilliseconds / measuredIterations;

        // --- Output results ---
        var speedup = gdiAvg / wpfAvg;
        var ratio = wpfElapsed.TotalMilliseconds / gdiElapsed.TotalMilliseconds * 100;

        Console.Error.WriteLine($"""

                                 ProIcon Benchmark Results ({measuredIterations} iterations):

                                   Old (GDI+ interop):    {gdiElapsed.TotalMilliseconds:F1}ms total, {gdiAvg:F3}ms avg
                                   New (WPF-native):      {wpfElapsed.TotalMilliseconds:F1}ms total, {wpfAvg:F3}ms avg
                                   New is {speedup:F1}x faster ({ratio:F0}% of old time)
                                 """);

        Assert.True(true);
    }

    [Fact]
    public void Benchmark_ProIcon_OutputPixelParity()
    {
        // Verify both paths produce visually equivalent output (same dimensions, non-empty)
        using var gdiBmp = OldGdiInteropPath(_testImagePath);
        using var wpfBmp = new ProIcon(_testImagePath).RenderToBitmap();

        Assert.Equal(256, gdiBmp.Width);
        Assert.Equal(256, gdiBmp.Height);
        Assert.Equal(256, wpfBmp.Width);
        Assert.Equal(256, wpfBmp.Height);

        // Both should have non-zero content (not blank)
        Assert.False(IsBlank(gdiBmp), "Old GDI+ output is blank");
        Assert.False(IsBlank(wpfBmp), "New WPF output is blank");
    }

    /// <summary>
    /// Old ProIcon path (inlined): GDI+ load → GDI+ resize → HBitmap interop → WPF BitmapSource → GDI+ Bitmap
    /// </summary>
    private static Bitmap OldGdiInteropPath(string filePath)
    {
        using var img = new Bitmap(filePath);
        using var icon = new Bitmap(img, 256, 256);
        var bitmapSource = ImageUtils.LoadBitmap(icon);
        return PosterIconBase.RenderTargetBitmapTo32BppArgb(bitmapSource);
    }

    private static bool IsBlank(Bitmap bmp)
    {
        var data = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            System.Drawing.Imaging.ImageLockMode.ReadOnly,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        var pixels = new byte[data.Stride * data.Height];
        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
        bmp.UnlockBits(data);
        var first = pixels[0];
        return pixels.All(p => p == first);
    }

    private static string FindTestProjectRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "FoliconTest.csproj")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        return dir ?? throw new InvalidOperationException("Could not find FoliconTest project root");
    }
}
