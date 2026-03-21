using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Keeps the app's shared accent brushes aligned with the user's Windows accent.
/// </summary>
public static class SystemAccentSync
{
    private static readonly Color FallbackAccent = Color.FromRgb(0x0F, 0x6C, 0xBD);

    private static Application? _application;
    private static bool _isAttached;

    public static void Attach(Application application)
    {
        if (_isAttached)
            return;

        _application = application;
        _isAttached = true;

        ApplyAccentPalette();
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public static void Detach()
    {
        if (!_isAttached)
            return;

        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _application = null;
        _isAttached = false;
    }

    private static void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.PropertyName) &&
            e.PropertyName != nameof(SystemParameters.WindowGlassColor))
        {
            return;
        }

        ScheduleApply();
    }

    private static void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e) =>
        ScheduleApply();

    private static void ScheduleApply()
    {
        var app = _application;
        if (app is null)
            return;

        _ = app.Dispatcher.BeginInvoke(ApplyAccentPalette);
    }

    private static void ApplyAccentPalette()
    {
        var app = _application;
        if (app is null)
            return;

        var accent = Normalize(ReadSystemAccent());

        UpdateBrush(app, "FluentAccentDefault", accent);
        UpdateBrush(app, "FluentAccentHover", Mix(accent, Colors.Black, 0.14));
        UpdateBrush(app, "FluentAccentPressed", Mix(accent, Colors.Black, 0.28));
        UpdateBrush(app, "FluentAccentLight1", Mix(accent, Colors.White, 0.18));
        UpdateBrush(app, "FluentAccentLight2", Mix(accent, Colors.White, 0.32));
        UpdateBrush(app, "FluentAccentLight3", Mix(accent, Colors.White, 0.46));
        UpdateBrush(app, "FluentAccentDark1", Mix(accent, Colors.Black, 0.12));
        UpdateBrush(app, "FluentAccentDark2", Mix(accent, Colors.Black, 0.24));
        UpdateBrush(app, "FluentAccentDark3", Mix(accent, Colors.Black, 0.38));
        UpdateBrush(app, "FluentTextSelectionBrush", accent, opacity: 0.4);
        UpdateGradient(app, "FluentAccentGradient", Mix(accent, Colors.White, 0.18), accent);
        UpdateGradient(app, "FluentAccentGradientHover", Mix(accent, Colors.White, 0.32), Mix(accent, Colors.Black, 0.14));
        UpdateGradient(app, "FluentAccentGradientPressed", Mix(accent, Colors.Black, 0.12), Mix(accent, Colors.Black, 0.28));
    }

    private static Color ReadSystemAccent()
    {
        if (TryReadRegistryAccent(out var registryAccent))
            return registryAccent;

        try
        {
            return SystemParameters.WindowGlassColor;
        }
        catch
        {
            return FallbackAccent;
        }
    }

    private static bool TryReadRegistryAccent(out Color accent)
    {
        foreach (var valueName in new[] { "ColorizationColor", "AccentColor" })
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
            if (TryConvertRegistryColor(key?.GetValue(valueName), out accent))
                return true;
        }

        accent = default;
        return false;
    }

    private static bool TryConvertRegistryColor(object? rawValue, out Color color)
    {
        switch (rawValue)
        {
            case int intValue:
                return TryCreateColor(unchecked((uint)intValue), out color);
            case long longValue:
                return TryCreateColor(unchecked((uint)longValue), out color);
            default:
                color = default;
                return false;
        }
    }

    private static bool TryCreateColor(uint raw, out Color color)
    {
        var accent = Color.FromArgb(
            0xFF,
            (byte)((raw >> 16) & 0xFF),
            (byte)((raw >> 8) & 0xFF),
            (byte)(raw & 0xFF));

        if (accent.R == 0 && accent.G == 0 && accent.B == 0)
        {
            color = default;
            return false;
        }

        color = accent;
        return true;
    }

    private static void UpdateBrush(Application app, string key, Color color, double? opacity = null)
    {
        if (app.TryFindResource(key) is SolidColorBrush existingBrush && !existingBrush.IsFrozen)
        {
            existingBrush.Color = color;
            if (opacity.HasValue)
                existingBrush.Opacity = opacity.Value;
            return;
        }

        var brush = new SolidColorBrush(color);
        if (opacity.HasValue)
            brush.Opacity = opacity.Value;

        app.Resources[key] = brush;
    }

    private static void UpdateGradient(Application app, string key, Color start, Color end)
    {
        if (app.TryFindResource(key) is LinearGradientBrush existingBrush && !existingBrush.IsFrozen)
        {
            EnsureGradient(existingBrush, start, end);
            return;
        }

        app.Resources[key] = CreateGradient(start, end);
    }

    private static LinearGradientBrush CreateGradient(Color start, Color end)
    {
        var brush = new LinearGradientBrush
        {
            StartPoint = new Point(0.5, 0),
            EndPoint = new Point(0.5, 1)
        };

        brush.GradientStops.Add(new GradientStop(start, 0));
        brush.GradientStops.Add(new GradientStop(end, 1));
        return brush;
    }

    private static void EnsureGradient(LinearGradientBrush brush, Color start, Color end)
    {
        if (brush.GradientStops.Count < 2)
        {
            brush.GradientStops.Clear();
            brush.GradientStops.Add(new GradientStop(start, 0));
            brush.GradientStops.Add(new GradientStop(end, 1));
        }
        else
        {
            brush.GradientStops[0].Color = start;
            brush.GradientStops[0].Offset = 0;
            brush.GradientStops[1].Color = end;
            brush.GradientStops[1].Offset = 1;
        }

        brush.StartPoint = new Point(0.5, 0);
        brush.EndPoint = new Point(0.5, 1);
    }

    private static Color Normalize(Color color) =>
        color.A == 0 ? FallbackAccent : Color.FromArgb(0xFF, color.R, color.G, color.B);

    private static Color Mix(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);

        return Color.FromArgb(
            0xFF,
            (byte)Math.Round(from.R + ((to.R - from.R) * amount)),
            (byte)Math.Round(from.G + ((to.G - from.G) * amount)),
            (byte)Math.Round(from.B + ((to.B - from.B) * amount)));
    }
}
