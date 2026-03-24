using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class Win11SharedFoundationStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedLayoutTokens_And_Styles_Should_Use_4Px_Grid_Spacing()
    {
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<Thickness x:Key=\"SetupRootPadding\">12</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"SetupStepperPadding\">16,12</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"FieldLabelSpacing\">0,0,0,8</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"ButtonPadding\">12,8</Thickness>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<Thickness       x:Key=\"TooltipPadding\">12,8,12,8</Thickness>", designSystem, StringComparison.Ordinal);

        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"SectionHeaderStyle\" TargetType=\"TextBlock\">",
            "<Setter Property=\"Margin\" Value=\"0,0,0,8\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"ToolbarButtonStyle\" TargetType=\"Button\"",
            "<Setter Property=\"Margin\" Value=\"0,0,8,0\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"InlineHelpButtonStyle\" TargetType=\"Button\"",
            "<Setter Property=\"Margin\" Value=\"8,0,0,0\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style TargetType=\"DataGridCell\">",
            "<Setter Property=\"Padding\" Value=\"12,8\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"SplitButtonFlyoutActionStyle\" TargetType=\"Button\" BasedOn=\"{x:Null}\">",
            "<Setter Property=\"Padding\" Value=\"12,8\"/>");

        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"QuickActionButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"Padding\" Value=\"12,8\"/>");
        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"EnterpriseActionButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"Padding\" Value=\"12,8\"/>");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentComboBoxItemStyle\" TargetType=\"ComboBoxItem\">",
            "<Setter Property=\"Padding\"             Value=\"12,8\"/>");
        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentToolbarStyle\" TargetType=\"Border\">",
            "<Setter Property=\"Padding\"            Value=\"16,12\"/>");
        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentStatusBarStyle\" TargetType=\"StatusBar\">",
            "<Setter Property=\"Padding\"             Value=\"12,8\"/>");
    }

    [Fact]
    public void SharedTypography_Should_Keep_Titles_Emphasized_And_Command_Text_Regular()
    {
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"PageTitleStyle\" TargetType=\"TextBlock\">",
            "<Setter Property=\"FontWeight\" Value=\"SemiBold\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"DialogTitleStyle\" TargetType=\"TextBlock\">",
            "<Setter Property=\"FontWeight\" Value=\"SemiBold\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"SectionHeaderStyle\" TargetType=\"TextBlock\">",
            "<Setter Property=\"FontWeight\" Value=\"SemiBold\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"FieldLabelStyle\" TargetType=\"TextBlock\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"FormRowLabelStyle\" TargetType=\"TextBlock\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"CaptionLabelStyle\" TargetType=\"TextBlock\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");

        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"SelectableUserButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");
        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"PosKeypadButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");
        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"QuickActionButtonStyle\" TargetType=\"Button\">",
            "FontWeight=\"Normal\"");
        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"EnterpriseActionButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");
        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"SegmentedButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");
        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"SegmentedFilterButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");
        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"ActiveFilterChipButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");
        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"SemanticTagChipTextStyle\" TargetType=\"TextBlock\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentAccentButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"FontWeight\"              Value=\"Normal\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"SplitButtonPrimarySegmentStyle\" TargetType=\"Button\" BasedOn=\"{x:Null}\">",
            "<Setter Property=\"FontWeight\" Value=\"Normal\"/>");
    }

    [Fact]
    public void SharedControlChrome_Should_Keep_The_4Px_Control_Corner_Radius()
    {
        var designSystem = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("<CornerRadius x:Key=\"FluentCornerSmall\">4</CornerRadius>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<CornerRadius x:Key=\"FluentCornerMedium\">4</CornerRadius>", designSystem, StringComparison.Ordinal);
        Assert.Contains("<CornerRadius x:Key=\"CornerRadiusMedium\">4</CornerRadius>", designSystem, StringComparison.Ordinal);

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentTextBoxStyle\" TargetType=\"TextBox\">",
            "CornerRadius=\"{StaticResource FluentCornerMedium}\"");
        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentAccentButtonStyle\" TargetType=\"Button\">",
            "CornerRadius=\"{StaticResource FluentCornerMedium}\"");
        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FlyoutMenuSurfaceStyle\"",
            "<Setter Property=\"CornerRadius\" Value=\"{StaticResource FluentCornerMedium}\"/>");
        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentMenuItemStyle\" TargetType=\"MenuItem\">",
            "CornerRadius=\"{StaticResource FluentCornerSmall}\"");
        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentContextMenuStyle\" TargetType=\"ContextMenu\">",
            "CornerRadius=\"{StaticResource FluentCornerMedium}\"");

        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"CardStyle\" TargetType=\"Border\">",
            "<Setter Property=\"CornerRadius\" Value=\"{StaticResource FluentCornerMedium}\"/>");
        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"SplitButtonFlyoutActionStyle\" TargetType=\"Button\" BasedOn=\"{x:Null}\">",
            "CornerRadius=\"{StaticResource FluentCornerMedium}\"");
    }

    private static void AssertStyleBlockContains(
        string content,
        string styleStart,
        params string[] expectedSnippets)
    {
        var start = content.IndexOf(styleStart, StringComparison.Ordinal);
        Assert.True(start >= 0, "Style block was not found: " + styleStart);

        var end = content.IndexOf("</Style>", start, StringComparison.Ordinal);
        Assert.True(end > start, "Style block did not terminate: " + styleStart);

        var block = content[start..(end + "</Style>".Length)];
        foreach (var snippet in expectedSnippets)
        {
            Assert.Contains(snippet, block, StringComparison.Ordinal);
        }
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
