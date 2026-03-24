using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Helpers;

/// <summary>
/// Keeps the shared token palette aligned with the Windows app theme preference.
/// </summary>
public static class SystemThemeSync
{
    private static Application? _application;
    private static IThemeService? _themeService;
    private static bool _isAttached;

    public static void Attach(Application application, IThemeService themeService)
    {
        if (_isAttached)
            return;

        _application = application;
        _themeService = themeService;
        _isAttached = true;

        ApplyCurrentTheme();
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
        _themeService = null;
        _isAttached = false;
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
        if (e.Category is not UserPreferenceCategory.Color and
            not UserPreferenceCategory.General and
            not UserPreferenceCategory.Accessibility)
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

        _ = app.Dispatcher.BeginInvoke(ApplyCurrentTheme);
    }

    private static void ApplyCurrentTheme()
    {
        var themeService = _themeService;
        if (themeService is null)
            return;

        themeService.SetTheme(ReadAppsUseLightTheme() ? AppTheme.Light : AppTheme.Dark);
    }

    private static bool ReadAppsUseLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1) ?? 1) != 0;
        }
        catch
        {
            return true;
        }
    }
}
