namespace StoreAssistantPro.Core.Printing;

public static class PrintPreviewZoomState
{
    private const double DefaultZoom = 100;
    private const double MinZoom = 80;
    private const double MaxZoom = 200;
    private static readonly Dictionary<string, double> ZoomLevels = new(StringComparer.OrdinalIgnoreCase);

    public static double Get(string? key)
    {
        var normalizedKey = NormalizeKey(key);
        return ZoomLevels.TryGetValue(normalizedKey, out var zoom)
            ? Clamp(zoom)
            : DefaultZoom;
    }

    public static void Set(string? key, double zoom)
    {
        ZoomLevels[NormalizeKey(key)] = Clamp(zoom);
    }

    internal static void Clear() => ZoomLevels.Clear();

    internal static double Clamp(double zoom) =>
        Math.Max(MinZoom, Math.Min(MaxZoom, Math.Round(zoom)));

    private static string NormalizeKey(string? key) =>
        string.IsNullOrWhiteSpace(key) ? "PrintPreview" : key.Trim();
}
