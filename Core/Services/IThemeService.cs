namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Manages application-wide theme switching between Light and Dark modes (#457).
/// </summary>
public interface IThemeService
{
    /// <summary>Current active theme.</summary>
    AppTheme CurrentTheme { get; }

    /// <summary>Toggles between Light and Dark themes.</summary>
    void ToggleTheme();

    /// <summary>Sets the theme explicitly.</summary>
    void SetTheme(AppTheme theme);

    /// <summary>Raised when the theme changes.</summary>
    event EventHandler<AppTheme>? ThemeChanged;
}

/// <summary>Application theme options.</summary>
public enum AppTheme
{
    Light,
    Dark
}
