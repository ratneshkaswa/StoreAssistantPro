using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class HighContrastStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void App_Should_Attach_And_Detach_Shared_HighContrastSync()
    {
        var appCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "App.xaml.cs"));

        Assert.Contains("HighContrastSync.Attach(this);", appCode, StringComparison.Ordinal);
        Assert.Contains("HighContrastSync.Detach();", appCode, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyHighContrastOverridesIfNeeded();", appCode, StringComparison.Ordinal);
    }

    [Fact]
    public void HighContrastSync_Should_Listen_For_Os_Mode_Changes_And_Apply_Shared_Override_Palette()
    {
        var helperCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "HighContrastSync.cs"));

        Assert.Contains("SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;", helperCode, StringComparison.Ordinal);
        Assert.Contains("SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;", helperCode, StringComparison.Ordinal);
        Assert.Contains("nameof(SystemParameters.HighContrast)", helperCode, StringComparison.Ordinal);
        Assert.Contains("Application.LoadComponent", helperCode, StringComparison.Ordinal);
        Assert.Contains(";component/Core/Styles/HighContrastOverrides.xaml", helperCode, StringComparison.Ordinal);
        Assert.Contains("typeof(HighContrastSync).Assembly.GetName().Name", helperCode, StringComparison.Ordinal);
        Assert.Contains("case (DrawingBrush targetDrawing, DrawingBrush sourceDrawing):", helperCode, StringComparison.Ordinal);
    }

    [Fact]
    public void HighContrastOverrides_Should_Cover_Modern_Win11_Surfaces_And_Accent_Tokens()
    {
        var overrides = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "HighContrastOverrides.xaml"));

        Assert.Contains("x:Key=\"AppBackgroundBrush\"", overrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"LayerFillColorDefault\"", overrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"LayerFillColorSecondary\"", overrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SubtleFillColorSecondary\"", overrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SubtleFillColorTertiary\"", overrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"ChromeAltFillColorSecondary\"", overrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"ControlAltFillColorSecondary\"", overrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"FluentTextSelectionBrush\"", overrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"FluentAccentGradient\"", overrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"FluentAccentGradientHover\"", overrides, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"FluentAccentGradientPressed\"", overrides, StringComparison.Ordinal);
    }

    [Fact]
    public void AccentSync_Should_Stand_Down_When_HighContrast_Is_Active()
    {
        var accentSync = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "SystemAccentSync.cs"));

        Assert.Contains("if (SystemParameters.HighContrast)", accentSync, StringComparison.Ordinal);
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
