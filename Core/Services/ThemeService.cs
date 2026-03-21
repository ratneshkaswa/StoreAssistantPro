using System.Windows;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Switches Light/Dark theme by swapping the DesignSystem resource dictionary (#457).
/// </summary>
public class ThemeService(ILogger<ThemeService> logger) : IThemeService
{
    private const string LightThemePath = "Core/Styles/DesignSystem.xaml";
    private const string DarkThemePath = "Core/Styles/DesignSystemDark.xaml";

    public AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

    public event EventHandler<AppTheme>? ThemeChanged;

    public void ToggleTheme() =>
        SetTheme(CurrentTheme == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);

    public void SetTheme(AppTheme theme)
    {
        if (theme == CurrentTheme)
            return;

        var app = Application.Current;
        if (app is null) return;

        var oldPath = theme == AppTheme.Dark ? LightThemePath : DarkThemePath;
        var newPath = theme == AppTheme.Dark ? DarkThemePath : LightThemePath;

        var oldUri = new Uri(oldPath, UriKind.Relative);
        var newUri = new Uri(newPath, UriKind.Relative);

        // Find and replace the DesignSystem dictionary in the merged dictionaries
        var merged = app.Resources.MergedDictionaries;
        ResourceDictionary? existing = null;

        foreach (var dict in merged)
        {
            if (dict.Source is not null &&
                dict.Source.OriginalString.Contains("DesignSystem", StringComparison.OrdinalIgnoreCase))
            {
                existing = dict;
                break;
            }
        }

        if (existing is not null)
        {
            var index = merged.IndexOf(existing);
            merged.RemoveAt(index);
            merged.Insert(index, new ResourceDictionary { Source = newUri });
        }

        CurrentTheme = theme;
        ThemeChanged?.Invoke(this, theme);
        logger.LogInformation("Theme changed to {Theme}", theme);
    }
}
