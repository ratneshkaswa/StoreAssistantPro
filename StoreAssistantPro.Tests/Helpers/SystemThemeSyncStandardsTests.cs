namespace StoreAssistantPro.Tests.Helpers;

public sealed class SystemThemeSyncStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void App_Should_Attach_And_Detach_SystemThemeSync()
    {
        var appCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "App.xaml.cs"));

        Assert.Contains("SystemThemeSync.Attach(this, themeService);", appCode, StringComparison.Ordinal);
        Assert.Contains("SystemThemeSync.Detach();", appCode, StringComparison.Ordinal);
    }

    [Fact]
    public void SystemThemeSync_Should_Listen_For_Windows_Theme_Changes()
    {
        var helperCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "SystemThemeSync.cs"));

        Assert.Contains("AppsUseLightTheme", helperCode, StringComparison.Ordinal);
        Assert.Contains("SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;", helperCode, StringComparison.Ordinal);
        Assert.Contains("SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;", helperCode, StringComparison.Ordinal);
        Assert.Contains("AppTheme.Light : AppTheme.Dark", helperCode, StringComparison.Ordinal);
    }

    [Fact]
    public void ThemeService_Should_Load_Dark_Override_Palette_And_Protect_HighContrast_Mode()
    {
        var serviceCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Services", "ThemeService.cs"));
        var darkOverrides = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DarkThemeOverrides.xaml"));

        Assert.Contains("DarkThemeOverrides.xaml", serviceCode, StringComparison.Ordinal);
        Assert.Contains("HighContrastSync.OverrideDefaultPalette", serviceCode, StringComparison.Ordinal);
        Assert.Contains("if (!SystemParameters.HighContrast)", serviceCode, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"AppBackgroundBrush\"", darkOverrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"FluentSurface\"", darkOverrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"FluentTextPrimary\"", darkOverrides, StringComparison.Ordinal);
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

        throw new InvalidOperationException(
            "Could not find solution root from " + AppContext.BaseDirectory);
    }
}
