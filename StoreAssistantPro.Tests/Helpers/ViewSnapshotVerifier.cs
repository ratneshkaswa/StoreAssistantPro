using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit.Sdk;

namespace StoreAssistantPro.Tests.Helpers;

internal sealed class ViewSnapshotBaseline
{
    public string AverageHash { get; set; } = string.Empty;

    public int DifferentPixels { get; set; }
}

internal static class ViewSnapshotVerifier
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static void AssertMatches(string key, FrameworkElement element, double width, double height)
    {
        var baselines = LoadBaselines();
        var snapshot = CaptureSnapshot(element, width, height);

        if (!baselines.TryGetValue(key, out var baseline))
        {
            throw new XunitException(
                $"Missing snapshot baseline for '{key}'. Actual AverageHash='{snapshot.AverageHash}', DifferentPixels={snapshot.DifferentPixels}.");
        }

        Assert.Equal(baseline.AverageHash, snapshot.AverageHash);

        var allowedDelta = Math.Max(200, (int)Math.Ceiling(baseline.DifferentPixels * 0.08));
        var delta = Math.Abs(snapshot.DifferentPixels - baseline.DifferentPixels);
        Assert.True(
            delta <= allowedDelta,
            $"Snapshot pixel count drifted for '{key}'. Expected {baseline.DifferentPixels}, actual {snapshot.DifferentPixels}, allowed ±{allowedDelta}.");
    }

    private static Dictionary<string, ViewSnapshotBaseline> LoadBaselines()
    {
        var path = Path.Combine(FindSolutionRoot(), "StoreAssistantPro.Tests", "Baselines", "ViewSnapshots.json");
        if (!File.Exists(path))
            return new Dictionary<string, ViewSnapshotBaseline>(StringComparer.OrdinalIgnoreCase);

        var content = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, ViewSnapshotBaseline>>(content, JsonOptions)
            ?? new Dictionary<string, ViewSnapshotBaseline>(StringComparer.OrdinalIgnoreCase);
    }

    private static (string AverageHash, int DifferentPixels) CaptureSnapshot(FrameworkElement element, double width, double height)
    {
        element.Measure(new Size(width, height));
        element.Arrange(new Rect(0, 0, width, height));
        element.UpdateLayout();

        var pixelWidth = Math.Max(1, (int)Math.Ceiling(width));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(height));
        var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);

        var stride = pixelWidth * 4;
        var pixels = new byte[stride * pixelHeight];
        bitmap.CopyPixels(pixels, stride, 0);

        return (ComputeAverageHash(pixels, pixelWidth, pixelHeight), CountDifferentPixels(pixels, pixelWidth, pixelHeight));
    }

    private static int CountDifferentPixels(byte[] pixels, int pixelWidth, int pixelHeight)
    {
        var stride = pixelWidth * 4;
        var baselineBlue = pixels[0];
        var baselineGreen = pixels[1];
        var baselineRed = pixels[2];
        var differentPixels = 0;

        for (var y = 0; y < pixelHeight; y++)
        {
            var rowStart = y * stride;
            for (var x = 0; x < pixelWidth; x++)
            {
                var index = rowStart + (x * 4);
                var blue = pixels[index];
                var green = pixels[index + 1];
                var red = pixels[index + 2];
                var alpha = pixels[index + 3];

                if (alpha == 0)
                    continue;

                if (Math.Abs(red - baselineRed) > 8 ||
                    Math.Abs(green - baselineGreen) > 8 ||
                    Math.Abs(blue - baselineBlue) > 8)
                {
                    differentPixels++;
                }
            }
        }

        return differentPixels;
    }

    private static string ComputeAverageHash(byte[] pixels, int pixelWidth, int pixelHeight)
    {
        Span<byte> samples = stackalloc byte[64];
        var sampleIndex = 0;
        long total = 0;

        for (var y = 0; y < 8; y++)
        {
            var sourceY = Math.Min(pixelHeight - 1, (int)Math.Round(y * (pixelHeight - 1) / 7d));
            for (var x = 0; x < 8; x++)
            {
                var sourceX = Math.Min(pixelWidth - 1, (int)Math.Round(x * (pixelWidth - 1) / 7d));
                var pixelIndex = (sourceY * pixelWidth + sourceX) * 4;
                var blue = pixels[pixelIndex];
                var green = pixels[pixelIndex + 1];
                var red = pixels[pixelIndex + 2];
                var luminance = (byte)Math.Clamp((red * 299 + green * 587 + blue * 114) / 1000, 0, 255);
                samples[sampleIndex++] = luminance;
                total += luminance;
            }
        }

        var average = total / 64d;
        ulong hash = 0;

        for (var i = 0; i < samples.Length; i++)
        {
            if (samples[i] >= average)
                hash |= 1UL << i;
        }

        return hash.ToString("X16");
    }

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0 ||
                Directory.GetFiles(dir, "*.slnx").Length > 0)
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not find solution root from " + AppContext.BaseDirectory);
    }
}
