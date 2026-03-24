using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Switches Light/Dark theme by applying shared token palettes in place (#457).
/// </summary>
public class ThemeService(ILogger<ThemeService> logger) : IThemeService
{
    private const string DarkThemeOverridesPath = "Core/Styles/DarkThemeOverrides.xaml";

    private IReadOnlyDictionary<string, Brush>? _lightPalette;
    private IReadOnlyDictionary<string, Brush>? _darkPalette;

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public event EventHandler<AppTheme>? ThemeChanged;

    public void ToggleTheme() =>
        SetTheme(CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);

    public void SetTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app is null)
        {
            CurrentTheme = theme;
            return;
        }

        EnsurePalettes(app);

        var targetPalette = theme == AppTheme.Dark
            ? _darkPalette
            : _lightPalette;

        if (targetPalette is null)
            return;

        HighContrastSync.OverrideDefaultPalette(targetPalette);

        if (!SystemParameters.HighContrast)
            ApplyPalette(app, targetPalette);

        if (theme == CurrentTheme)
            return;

        CurrentTheme = theme;
        ThemeChanged?.Invoke(this, theme);
        logger.LogInformation("Theme changed to {Theme}", theme);
    }

    private void EnsurePalettes(Application application)
    {
        if (_darkPalette is null)
        {
            var dictionary = (ResourceDictionary)Application.LoadComponent(
                new Uri(DarkThemeOverridesPath, UriKind.Relative));

            _darkPalette = dictionary
                .Keys
                .OfType<string>()
                .Where(key => dictionary[key] is Brush)
                .ToDictionary(
                    key => key,
                    key => ((Brush)dictionary[key]).CloneCurrentValue(),
                    StringComparer.Ordinal);
        }

        if (_lightPalette is not null || _darkPalette is null)
            return;

        _lightPalette = _darkPalette.Keys
            .Where(key => application.TryFindResource(key) is Brush)
            .ToDictionary(
                key => key,
                key => ((Brush)application.FindResource(key)).CloneCurrentValue(),
                StringComparer.Ordinal);
    }

    private static void ApplyPalette(Application application, IReadOnlyDictionary<string, Brush> palette)
    {
        foreach (var (key, sourceBrush) in palette)
        {
            if (application.TryFindResource(key) is Brush existingBrush && !existingBrush.IsFrozen)
            {
                CopyBrush(existingBrush, sourceBrush);
                continue;
            }

            application.Resources[key] = sourceBrush.CloneCurrentValue();
        }
    }

    private static void CopyBrush(Brush target, Brush source)
    {
        switch (target, source)
        {
            case (SolidColorBrush targetSolid, SolidColorBrush sourceSolid):
                targetSolid.Color = sourceSolid.Color;
                targetSolid.Opacity = sourceSolid.Opacity;
                break;

            case (LinearGradientBrush targetGradient, LinearGradientBrush sourceGradient):
                targetGradient.StartPoint = sourceGradient.StartPoint;
                targetGradient.EndPoint = sourceGradient.EndPoint;
                targetGradient.MappingMode = sourceGradient.MappingMode;
                targetGradient.ColorInterpolationMode = sourceGradient.ColorInterpolationMode;
                targetGradient.SpreadMethod = sourceGradient.SpreadMethod;
                targetGradient.Opacity = sourceGradient.Opacity;
                targetGradient.GradientStops.Clear();

                foreach (var stop in sourceGradient.GradientStops)
                    targetGradient.GradientStops.Add(stop.CloneCurrentValue());
                break;

            case (DrawingBrush targetDrawing, DrawingBrush sourceDrawing):
                targetDrawing.AlignmentX = sourceDrawing.AlignmentX;
                targetDrawing.AlignmentY = sourceDrawing.AlignmentY;
                targetDrawing.Stretch = sourceDrawing.Stretch;
                targetDrawing.TileMode = sourceDrawing.TileMode;
                targetDrawing.Viewbox = sourceDrawing.Viewbox;
                targetDrawing.ViewboxUnits = sourceDrawing.ViewboxUnits;
                targetDrawing.Viewport = sourceDrawing.Viewport;
                targetDrawing.ViewportUnits = sourceDrawing.ViewportUnits;
                targetDrawing.Opacity = sourceDrawing.Opacity;
                targetDrawing.Drawing = sourceDrawing.Drawing?.CloneCurrentValue();
                break;
        }
    }
}
