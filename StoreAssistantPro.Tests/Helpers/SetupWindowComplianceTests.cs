using Xunit;
using System.Text.RegularExpressions;

namespace StoreAssistantPro.Tests.Helpers;

public class SetupWindowComplianceTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static string FindSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (Directory.GetFiles(dir, "*.sln").Length > 0 || Directory.GetFiles(dir, "*.slnx").Length > 0)
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not find solution root from " + AppContext.BaseDirectory);
    }

    [Fact]
    public void SetupWindow_Should_KeepScrollableContent_WhenFieldsGrow()
    {
        var pagesDir = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupPages");
        var pages = Directory.GetFiles(pagesDir, "*.xaml");

        foreach (var page in pages)
        {
            var xaml = File.ReadAllText(page);
            Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml);
        }
    }

    [Fact]
    public void SetupWindow_Should_NotUseWatermarkAdorners()
    {
        var pagesDir = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views");
        var xamlFiles = Directory.EnumerateFiles(pagesDir, "*.xaml", SearchOption.AllDirectories);

        foreach (var file in xamlFiles)
        {
            var xaml = File.ReadAllText(file);
            Assert.DoesNotContain("h:Watermark.Text=", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SetupWindow_Should_ProvideAccessibilityLabels()
    {
        var pagesDir = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupPages");
        var pages = Directory.GetFiles(pagesDir, "*.xaml");
        var allXaml = string.Join("\n", pages.Select(File.ReadAllText));

        Assert.Contains("AutomationProperties.LabeledBy=\"{Binding ElementName=FirmNameLabel}\"", allXaml);
        Assert.Contains("AutomationProperties.LabeledBy=\"{Binding ElementName=MasterPinLabel}\"", allXaml);
        Assert.Contains("AutomationProperties.LabeledBy=\"{Binding ElementName=UserConfirmLabel}\"", allXaml);
        Assert.Contains("AutomationProperties.LabeledBy=\"{Binding ElementName=AdminConfirmLabel}\"", allXaml);
        Assert.Contains("AutomationProperties.LabeledBy=\"{Binding ElementName=MasterConfirmLabel}\"", allXaml);
    }

    [Fact]
    public void SetupWindow_Should_SubmitOnEnter_And_FocusSecurityFieldMapping()
    {
        var windowFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var windowCodeBehind = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml.cs");
        var securityPageFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupPages", "SecuritySettingsPage.xaml");

        var windowXaml = File.ReadAllText(windowFile);
        var windowCs = File.ReadAllText(windowCodeBehind);
        var securityXaml = File.ReadAllText(securityPageFile);

        Assert.Contains("h:KeyboardNav.DefaultCommand=\"{Binding SaveCommand}\"", windowXaml, StringComparison.Ordinal);
        Assert.Contains("[\"AdminPin\"] = (\"Security\", \"AdminPinBox\")", windowCs, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AdminPinBox\"", securityXaml, StringComparison.Ordinal);
        Assert.Contains("TabIndex=\"6\"", securityXaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupWindow_Should_UseResponsiveFooterAndProgressCardLayout()
    {
        var windowFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var xaml = File.ReadAllText(windowFile);

        Assert.Contains("<WrapPanel Grid.Column=\"1\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TextWrapping=\"Wrap\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"180\" MaxWidth=\"260\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<StackPanel Grid.Column=\"1\" Orientation=\"Horizontal\" HorizontalAlignment=\"Right\">", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupWindow_Should_UseAdaptiveSecurityColumnsAndWrappingHintLanes()
    {
        var securityFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupPages", "SecuritySettingsPage.xaml");
        var xaml = File.ReadAllText(securityFile);

        Assert.Contains("ColumnDefinition Width=\"Auto\" MinWidth=\"132\"", xaml, StringComparison.Ordinal);
        Assert.Contains("ColumnDefinition Width=\"Auto\" MinWidth=\"144\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SecurityRolePinColumnWidth", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SecurityRecoveryPinColumnWidth", xaml, StringComparison.Ordinal);
        Assert.Contains("Orientation=\"Vertical\"", xaml, StringComparison.Ordinal);
        Assert.True(Regex.Matches(xaml, "<WrapPanel Orientation=\"Horizontal\">").Count >= 3);
    }

    [Fact]
    public void SetupWindow_Should_UseAdaptiveTwoPaneLayoutWithStackBreakpoint()
    {
        var windowFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var codeBehindFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml.cs");
        var xaml = File.ReadAllText(windowFile);
        var cs = File.ReadAllText(codeBehindFile);

        Assert.Contains("x:Name=\"SetupContentGrid\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FirmPane\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SecurityPane\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AdaptiveStackBreakpointWidth = 1240d", cs, StringComparison.Ordinal);
        Assert.Contains("UpdateAdaptiveLayout()", cs, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupWindow_Should_AvoidTrimmingInSetupHelpAndPinHintStyles()
    {
        var stylesFile = Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml");
        var styles = File.ReadAllText(stylesFile);
        var options = RegexOptions.CultureInvariant;

        Assert.Contains("x:Key=\"SetupSectionHintTextStyle\"", styles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SetupSidebarNavSubtitleStyle\"", styles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SetupPinWarningTextStyle\"", styles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SetupPinInlineHintStyle\"", styles, StringComparison.Ordinal);
        Assert.True(Regex.Matches(styles, "TextWrapping\" Value=\"Wrap\"").Count >= 4);

        // No character ellipsis in setup-specific helper/hint styles.
        var sectionHintStyle = Regex.Match(styles, "<Style x:Key=\"SetupSectionHintTextStyle\"[\\s\\S]*?</Style>", options).Value;
        var sidebarSubtitleStyle = Regex.Match(styles, "<Style x:Key=\"SetupSidebarNavSubtitleStyle\"[\\s\\S]*?</Style>", options).Value;
        var pinWarningStyle = Regex.Match(styles, "<Style x:Key=\"SetupPinWarningTextStyle\"[\\s\\S]*?</Style>", options).Value;
        var pinInlineHintStyle = Regex.Match(styles, "<Style x:Key=\"SetupPinInlineHintStyle\"[\\s\\S]*?</Style>", options).Value;

        Assert.DoesNotContain("TextTrimming", sectionHintStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("TextTrimming", sidebarSubtitleStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("TextTrimming", pinWarningStyle, StringComparison.Ordinal);
        Assert.DoesNotContain("TextTrimming", pinInlineHintStyle, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupWindow_Should_UseShrinkSafeSetupWindowTokens()
    {
        var designTokensFile = Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml");
        var tokens = File.ReadAllText(designTokensFile);

        Assert.Contains("<sys:Double x:Key=\"SetupMinWidth\">920</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"SetupMinHeight\">620</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"SecurityRolePinBoxWidth\">116</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"SecurityMasterPinBoxWidth\">128</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"FieldWidthStandard\">144</sys:Double>", tokens, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupWindow_Should_ShowOnlyEssentialSections()
    {
        var file = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var xaml = File.ReadAllText(file);

        Assert.Contains("x:Name=\"FirmContentFrame\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SecurityContentFrame\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"NavFirm\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"NavSecurity\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"NavTax\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"NavRegional\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"NavBackup\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"NavSystem\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Firm profile", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Security settings", xaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Codebase_Should_NotUseMicaFlag()
    {
        var forbidden = "use" + "MicaAlt";
        var csFiles = Directory.EnumerateFiles(SolutionRoot, "*.cs", SearchOption.AllDirectories);
        foreach (var file in csFiles)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }
}
