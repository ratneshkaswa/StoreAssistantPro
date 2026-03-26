using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Printing;

public static class PrintPreviewZoomState
{
    private const double DefaultZoom = 100;
    private const double MinZoom = 80;
    private const double MaxZoom = 200;

    public static double Get(string? key)
    {
        var normalizedKey = NormalizeKey(key);
        return Clamp(UserPreferencesStore.GetPrintPreviewZoom(normalizedKey, DefaultZoom));
    }

    public static void Set(string? key, double zoom)
    {
        UserPreferencesStore.SetPrintPreviewZoom(NormalizeKey(key), Clamp(zoom));
    }

    internal static void Clear() =>
        UserPreferencesStore.Update(state => state.PrintPreviewZoomLevels.Clear());

    internal static double Clamp(double zoom) =>
        Math.Max(MinZoom, Math.Min(MaxZoom, Math.Round(zoom)));

    private static string NormalizeKey(string? key) =>
        string.IsNullOrWhiteSpace(key) ? "PrintPreview" : key.Trim();
}
