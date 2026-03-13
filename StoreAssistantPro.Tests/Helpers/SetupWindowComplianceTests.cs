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
        var windowFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var xaml = File.ReadAllText(windowFile);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml);
    }

    [Fact]
    public void SetupWindow_Should_Use_Explicit_Example_Watermarks()
    {
        var firmProfileFile = Path.Combine(
            SolutionRoot,
            "Modules",
            "Authentication",
            "Views",
            "SetupPages",
            "FirmProfilePage.xaml");
        var securityFile = Path.Combine(
            SolutionRoot,
            "Modules",
            "Authentication",
            "Views",
            "SetupPages",
            "SecuritySettingsPage.xaml");

        var firmProfileXaml = File.ReadAllText(firmProfileFile);
        var securityXaml = File.ReadAllText(securityFile);

        Assert.Contains("h:Watermark.Text=\"e.g. Sonali Collection\"", firmProfileXaml, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. 12 Bapu Bazaar, Jaipur\"", firmProfileXaml, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. Rajasthan\"", firmProfileXaml, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. 9876543210\"", firmProfileXaml, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. 302001\"", firmProfileXaml, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. hello@yourstore.in\"", firmProfileXaml, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. 2480\"", securityXaml, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. 1357\"", securityXaml, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. 482913\"", securityXaml, StringComparison.Ordinal);
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
    public void SetupWindow_Should_UseCleanFooterWithoutHelperCopy()
    {
        var windowFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var xaml = File.ReadAllText(windowFile);

        Assert.Contains("<WrapPanel Grid.Column=\"1\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("All fields are editable. Press Enter to move next.", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Business and security details only.", xaml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<StackPanel Grid.Column=\"1\" Orientation=\"Horizontal\" HorizontalAlignment=\"Right\">", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupWindow_Should_UseCompactTwoColumnSecurityLayout()
    {
        var securityFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupPages", "SecuritySettingsPage.xaml");
        var xaml = File.ReadAllText(securityFile);

        Assert.Contains("<ColumnDefinition Width=\"*\"/>", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AdminPinBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"UserPinBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"MasterPinBox\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"MasterPinConfirmBox\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SecurityRoleLabelWidth", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Confirm\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupWindow_Should_UseSingleContinuousSetupView()
    {
        var windowFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var codeBehindFile = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml.cs");
        var xaml = File.ReadAllText(windowFile);
        var cs = File.ReadAllText(codeBehindFile);

        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FirmProfileSection\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SecuritySettingsSection\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Frame", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"SetupContentGrid\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"FirmPane\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"SecurityPane\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateAdaptiveLayout()", cs, StringComparison.Ordinal);
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

        Assert.Contains("<sys:Double x:Key=\"SetupMinWidth\">1000</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"SetupMinHeight\">820</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"SecurityRolePinBoxWidth\">116</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"SecurityMasterPinBoxWidth\">128</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"FieldWidthStandard\">144</sys:Double>", tokens, StringComparison.Ordinal);
    }

    [Fact]
    public void SetupWindow_Should_ShowOnlyEssentialSections()
    {
        var file = Path.Combine(SolutionRoot, "Modules", "Authentication", "Views", "SetupWindow.xaml");
        var xaml = File.ReadAllText(file);

        Assert.Contains("x:Name=\"FirmProfileSection\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SecuritySettingsSection\"", xaml, StringComparison.Ordinal);
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
