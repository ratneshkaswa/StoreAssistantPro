using System.Text.RegularExpressions;

namespace StoreAssistantPro.Tests.Helpers;

/// <summary>
/// Static XAML scanner that verifies PredictiveFocusBehavior is wired
/// into the global Window style and that AutoFocus.IsEnabled is not
/// duplicated on Windows (which would cause double-focus on load).
/// </summary>
public partial class PredictiveFocusBehaviorComplianceTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0
                || Directory.GetFiles(dir, "*.slnx").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Cannot find solution root.");
    }

    // ── GlobalStyles.xaml wiring ────────────────────────────────────

    [Fact]
    public void GlobalStyles_WindowStyle_HasPredictiveFocusBehavior()
    {
        var globalStyles = Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml");
        Assert.True(File.Exists(globalStyles), "GlobalStyles.xaml not found");

        var content = File.ReadAllText(globalStyles);

        Assert.Contains(
            "PredictiveFocusBehavior.IsEnabled",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalStyles_WindowStyle_DoesNotHaveAutoFocusEnabled()
    {
        var globalStyles = Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml");
        var content = File.ReadAllText(globalStyles);

        // AutoFocus.IsEnabled should NOT be in the Window style setter.
        // It's been replaced by PredictiveFocusBehavior.IsEnabled.
        // The opt-out comment referencing AutoFocus.IsEnabled is allowed;
        // only the Setter line is prohibited.
        var setterPattern = SetterAutoFocusRegex();
        var matches = setterPattern.Matches(content);

        Assert.True(matches.Count == 0,
            "Window style should use PredictiveFocusBehavior.IsEnabled, not AutoFocus.IsEnabled. " +
            $"Found {matches.Count} Setter(s) still using AutoFocus.IsEnabled.");
    }

    // ── Behavior file exists ────────────────────────────────────────

    [Fact]
    public void PredictiveFocusBehavior_FileExists()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "PredictiveFocusBehavior.cs");
        Assert.True(File.Exists(path), "PredictiveFocusBehavior.cs not found");
    }

    [Fact]
    public void PredictiveFocusBehavior_HasIsEnabledProperty()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "PredictiveFocusBehavior.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("IsEnabledProperty", content, StringComparison.Ordinal);
        Assert.Contains("RegisterAttached", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PredictiveFocusBehavior_SubscribesToFocusHintChangedEvent()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "PredictiveFocusBehavior.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("FocusHintChangedEvent", content, StringComparison.Ordinal);
        Assert.Contains("Subscribe", content, StringComparison.Ordinal);
        Assert.Contains("Unsubscribe", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PredictiveFocusBehavior_DispatchesAtInputPriority()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "PredictiveFocusBehavior.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("DispatcherPriority.Input", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PredictiveFocusBehavior_HandlesAllFocusStrategies()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "PredictiveFocusBehavior.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("FocusStrategy.FirstInput", content, StringComparison.Ordinal);
        Assert.Contains("FocusStrategy.Named", content, StringComparison.Ordinal);
        Assert.Contains("FocusStrategy.Preserve", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PredictiveFocusBehavior_CleansUpOnUnloaded()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "PredictiveFocusBehavior.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("Unloaded", content, StringComparison.Ordinal);
        Assert.Contains("Detach", content, StringComparison.Ordinal);
    }

    // ── App.Services is exposed for behavior resolution ─────────────

    [Fact]
    public void App_ExposesServicesProperty()
    {
        var path = Path.Combine(SolutionRoot, "App.xaml.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("public IServiceProvider? Services", content, StringComparison.Ordinal);
    }

    // ── AutoFocus.cs is NOT removed (still used by FormCardStyle) ───

    [Fact]
    public void AutoFocus_StillExists()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "AutoFocus.cs");
        Assert.True(File.Exists(path),
            "AutoFocus.cs should still exist — it's used by OnReveal for form containers.");
    }

    // ── Safety guard integration ─────────────────────────────────────

    [Fact]
    public void PredictiveFocusBehavior_ConsultsSafetyGuard()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "PredictiveFocusBehavior.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("IFocusSafetyGuard", content, StringComparison.Ordinal);
        Assert.Contains("CanExecute", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PredictiveFocusBehavior_DetectsMouseClicks()
    {
        var path = Path.Combine(SolutionRoot, "Core", "Helpers", "PredictiveFocusBehavior.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("PreviewMouseDown", content, StringComparison.Ordinal);
        Assert.Contains("SignalUserClick", content, StringComparison.Ordinal);
    }

    [Fact]
    public void FocusSafetyGuard_IsRegistered()
    {
        var path = Path.Combine(SolutionRoot, "HostingExtensions.cs");
        var content = File.ReadAllText(path);

        Assert.Contains("IFocusSafetyGuard", content, StringComparison.Ordinal);
        Assert.Contains("FocusSafetyGuard", content, StringComparison.Ordinal);
    }

    // ── Regex ────────────────────────────────────────────────────────

    [GeneratedRegex(
        @"<Setter\s+Property=""h:AutoFocus\.IsEnabled""\s+Value=""True""\s*/>",
        RegexOptions.IgnoreCase)]
    private static partial Regex SetterAutoFocusRegex();
}
