using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Keeps the shared resource tokens aligned with the OS high-contrast mode.
/// </summary>
public static class HighContrastSync
{
    private const string HighContrastOverridesPath = "Core/Styles/HighContrastOverrides.xaml";

    private static Application? _application;
    private static bool _isAttached;
    private static IReadOnlyDictionary<string, Brush>? _defaultPalette;
    private static IReadOnlyDictionary<string, Brush>? _highContrastPalette;

    public static void Attach(Application application)
    {
        if (_isAttached)
            return;

        _application = application;
        _defaultPalette = CapturePalette(application, GetHighContrastPalette().Keys);
        _isAttached = true;

        ApplyCurrentMode();
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

    public static void OverrideDefaultPalette(IReadOnlyDictionary<string, Brush> palette)
    {
        _defaultPalette = palette.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.CloneCurrentValue(),
            StringComparer.Ordinal);
    }

    private static void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.PropertyName) &&
            e.PropertyName != nameof(SystemParameters.HighContrast))
        {
            return;
        }

        ScheduleApply();
    }

    private static void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category is not UserPreferenceCategory.Accessibility and
            not UserPreferenceCategory.Color and
            not UserPreferenceCategory.General)
        {
            return;
        }

        ScheduleApply();
    }

    private static void ScheduleApply()
    {
        var app = _application;
        if (app is null)
            return;

        _ = app.Dispatcher.BeginInvoke(ApplyCurrentMode);
    }

    private static void ApplyCurrentMode()
    {
        var app = _application;
        if (app is null || _defaultPalette is null)
            return;

        var palette = SystemParameters.HighContrast
            ? GetHighContrastPalette()
            : _defaultPalette;

        ApplyPalette(app, palette);
    }

    private static IReadOnlyDictionary<string, Brush> GetHighContrastPalette()
    {
        if (_highContrastPalette is not null)
            return _highContrastPalette;

        var dictionary = (ResourceDictionary)Application.LoadComponent(
            new Uri(HighContrastOverridesPath, UriKind.Relative));

        _highContrastPalette = dictionary
            .Keys
            .OfType<string>()
            .Where(key => dictionary[key] is Brush)
            .ToDictionary(
                key => key,
                key => ((Brush)dictionary[key]).CloneCurrentValue(),
                StringComparer.Ordinal);

        return _highContrastPalette;
    }

    private static IReadOnlyDictionary<string, Brush> CapturePalette(
        Application application,
        IEnumerable<string> keys)
    {
        var palette = new Dictionary<string, Brush>(StringComparer.Ordinal);

        foreach (var key in keys)
        {
            if (application.TryFindResource(key) is Brush brush)
                palette[key] = brush.CloneCurrentValue();
        }

        return palette;
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

            default:
                break;
        }
    }
}
