using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Maps a tag/category/payment label to a stable tinted chip palette.
/// ConverterParameter selects the returned brush:
/// <c>Background</c> (default), <c>Border</c>, or <c>Foreground</c>.
/// </summary>
public sealed class TagChipBrushConverter : IValueConverter
{
    private static readonly TagChipPalette NeutralPalette = new(
        CreateBrush("#F3F4F6"),
        CreateBrush("#D1D5DB"),
        CreateBrush("#374151"));

    private static readonly TagChipPalette AccentPalette = new(
        CreateBrush("#EAF3FF"),
        CreateBrush("#B7D7FF"),
        CreateBrush("#0B57A4"));

    private static readonly TagChipPalette SuccessPalette = new(
        CreateBrush("#ECFDF5"),
        CreateBrush("#A7F3D0"),
        CreateBrush("#0F7B0F"));

    private static readonly TagChipPalette WarningPalette = new(
        CreateBrush("#FFF7ED"),
        CreateBrush("#FED7AA"),
        CreateBrush("#9A3412"));

    private static readonly TagChipPalette PlumPalette = new(
        CreateBrush("#F5F3FF"),
        CreateBrush("#DDD6FE"),
        CreateBrush("#6D28D9"));

    private static readonly TagChipPalette TealPalette = new(
        CreateBrush("#ECFEFF"),
        CreateBrush("#A5F3FC"),
        CreateBrush("#0F766E"));

    private static readonly TagChipPalette[] FallbackPalettes =
    [
        AccentPalette,
        SuccessPalette,
        WarningPalette,
        PlumPalette,
        TealPalette
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var palette = ResolvePalette(value?.ToString());
        var part = parameter as string ?? "Background";

        return part switch
        {
            "Border" => palette.Border,
            "Foreground" => palette.Foreground,
            _ => palette.Background
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();

    private static TagChipPalette ResolvePalette(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return NeutralPalette;

        var normalized = text.Trim().ToLowerInvariant();

        if (normalized.Contains("cash", StringComparison.Ordinal) ||
            normalized.Contains("paid", StringComparison.Ordinal))
        {
            return SuccessPalette;
        }

        if (normalized.Contains("credit", StringComparison.Ordinal) ||
            normalized.Contains("due", StringComparison.Ordinal) ||
            normalized.Contains("pending", StringComparison.Ordinal))
        {
            return WarningPalette;
        }

        if (normalized.Contains("upi", StringComparison.Ordinal) ||
            normalized.Contains("online", StringComparison.Ordinal))
        {
            return AccentPalette;
        }

        if (normalized.Contains("bank", StringComparison.Ordinal) ||
            normalized.Contains("transfer", StringComparison.Ordinal) ||
            normalized.Contains("user", StringComparison.Ordinal))
        {
            return TealPalette;
        }

        if (normalized.Contains("card", StringComparison.Ordinal) ||
            normalized.Contains("admin", StringComparison.Ordinal) ||
            normalized.Contains("service", StringComparison.Ordinal))
        {
            return PlumPalette;
        }

        return FallbackPalettes[ComputeStableIndex(normalized)];
    }

    private static int ComputeStableIndex(string text)
    {
        var hash = 17;
        foreach (var ch in text)
            hash = (hash * 31) + ch;

        return Math.Abs(hash) % FallbackPalettes.Length;
    }

    private static SolidColorBrush CreateBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)!);
        brush.Freeze();
        return brush;
    }

    private sealed record TagChipPalette(
        SolidColorBrush Background,
        SolidColorBrush Border,
        SolidColorBrush Foreground);
}
