using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class PixelLayoutStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedDialogShell_Should_Enable_Rounding_And_PixelSnapping()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Base", "BaseDialogWindow.cs"));

        Assert.Contains("UseLayoutRounding = true;", content, StringComparison.Ordinal);
        Assert.Contains("SnapsToDevicePixels = true;", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Modules\\Authentication\\Views\\LoginView.xaml")]
    [InlineData("Modules\\MainShell\\Views\\MainWindow.xaml")]
    [InlineData("Modules\\MainShell\\Views\\WorkspaceView.xaml")]
    public void ShellRoots_Should_OptInto_Rounding_And_PixelSnapping(string relativePath)
    {
        var content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains("UseLayoutRounding=\"True\"", content, StringComparison.Ordinal);
        Assert.Contains("SnapsToDevicePixels=\"True\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void ToggleSwitchTemplate_Should_Use_Crisp_Layout_Settings()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "ToggleSwitch.xaml"));

        Assert.Contains("<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>", content, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>", content, StringComparison.Ordinal);
        Assert.Contains("<Grid Width=\"40\"", content, StringComparison.Ordinal);
        Assert.Contains("UseLayoutRounding=\"True\"", content, StringComparison.Ordinal);
        Assert.Contains("SnapsToDevicePixels=\"True\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedCardStyles_Should_Enable_Rounding_And_PixelSnapping()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Style x:Key=\"FluentCardStyle\" TargetType=\"Border\">", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"SetupFlatCardStyle\" TargetType=\"Border\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>", globalStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedMicroSurfaceTemplates_Should_Enable_Crisp_Layout_Settings()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Style TargetType=\"{x:Type controls:NumberBox}\">", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"UseLayoutRounding\"       Value=\"True\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Grid UseLayoutRounding=\"True\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"{x:Type controls:LoadingOverlay}\">", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"controls:InlineTipBanner\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"controls:EmptyStateOverlay\">", globalStyles, StringComparison.Ordinal);
        Assert.Contains("UseLayoutRounding=\"True\"", globalStyles, StringComparison.Ordinal);
        Assert.Contains("SnapsToDevicePixels=\"True\"", globalStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedFormFieldStyles_Should_Enable_Rounding_And_PixelSnapping()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentTextBoxStyle\" TargetType=\"TextBox\">",
            "<Style x:Key=\"FluentComboBoxStyle\" TargetType=\"ComboBox\">",
            "<Setter Property=\"UseLayoutRounding\"       Value=\"True\"/>",
            "<Grid UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentComboBoxStyle\" TargetType=\"ComboBox\">",
            "<Style x:Key=\"FluentScrollBarStyle\" TargetType=\"ScrollBar\">",
            "<Setter Property=\"UseLayoutRounding\"       Value=\"True\"/>",
            "<Grid UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentPasswordBoxStyle\" TargetType=\"PasswordBox\">",
            "<Style x:Key=\"FluentButtonBaseStyle\" TargetType=\"Button\">",
            "<Setter Property=\"UseLayoutRounding\"       Value=\"True\"/>",
            "<Grid UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentCheckBoxStyle\" TargetType=\"CheckBox\">",
            "<Style x:Key=\"FluentDataGridStyle\" TargetType=\"DataGrid\">",
            "<Setter Property=\"UseLayoutRounding\"   Value=\"True\"/>",
            "<Grid UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentRadioButtonStyle\" TargetType=\"RadioButton\">",
            "<Style x:Key=\"FluentDatePickerStyle\" TargetType=\"DatePicker\">",
            "<Setter Property=\"UseLayoutRounding\"   Value=\"True\"/>",
            "<Grid UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentDatePickerStyle\" TargetType=\"DatePicker\">",
            "<Style TargetType=\"{x:Type controls:InfoBar}\">",
            "<Setter Property=\"UseLayoutRounding\"       Value=\"True\"/>",
            "<Grid UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");
    }

    [Fact]
    public void SharedFeedbackAndNavigationStyles_Should_Enable_Crisp_Layout_Settings()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentToolTipStyle\" TargetType=\"ToolTip\">",
            "<Style x:Key=\"FluentFocusVisualStyle\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style TargetType=\"{x:Type controls:InfoBar}\">",
            "<Style TargetType=\"{x:Type controls:ProgressRing}\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Grid UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style TargetType=\"{x:Type controls:ProgressRing}\">",
            "<Style TargetType=\"{x:Type controls:LoadingOverlay}\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"");

        Assert.Contains("<Style TargetType=\"{x:Type controls:ToastHost}\">", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{TemplateBinding Toasts}\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>", fluentTheme, StringComparison.Ordinal);

        AssertStyleBlockContains(
            fluentTheme,
            "<Style TargetType=\"{x:Type controls:BreadcrumbBar}\">",
            "<Style TargetType=\"{x:Type controls:BreadcrumbBarItem}\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style TargetType=\"{x:Type controls:BreadcrumbBarItem}\">",
            "</ResourceDictionary>",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"");

        AssertStyleBlockContains(
            globalStyles,
            "<Style TargetType=\"controls:AppBranding\">",
            "</ResourceDictionary>",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"");
    }

    [Fact]
    public void SharedCommandAndDataSurfaceStyles_Should_Enable_Crisp_Layout_Settings()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentComboBoxItemStyle\" TargetType=\"ComboBoxItem\">",
            "<Style x:Key=\"FluentPasswordBoxStyle\" TargetType=\"PasswordBox\">",
            "<Setter Property=\"UseLayoutRounding\"   Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentAccentButtonStyle\" TargetType=\"Button\">",
            "<Style x:Key=\"FluentSubtleButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"UseLayoutRounding\"       Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentStandardButtonStyle\" TargetType=\"Button\">",
            "<Style x:Key=\"FluentGroupBoxStyle\" TargetType=\"GroupBox\">",
            "<Setter Property=\"UseLayoutRounding\"       Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentGroupBoxStyle\" TargetType=\"GroupBox\">",
            "<Style x:Key=\"FluentMenuBarStyle\" TargetType=\"Menu\">",
            "<Setter Property=\"UseLayoutRounding\"   Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentMenuBarStyle\" TargetType=\"Menu\">",
            "<Style x:Key=\"FluentMenuSeparatorStyle\" TargetType=\"Separator\">",
            "<Setter Property=\"UseLayoutRounding\"   Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentMenuBarItemStyle\" TargetType=\"MenuItem\">",
            "<Style x:Key=\"FluentStatusBarStyle\" TargetType=\"StatusBar\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentStatusBarStyle\" TargetType=\"StatusBar\">",
            "<Style x:Key=\"FluentStatusBarSeparatorStyle\" TargetType=\"Border\">",
            "<Setter Property=\"UseLayoutRounding\"   Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentContextMenuStyle\" TargetType=\"ContextMenu\">",
            "<Style x:Key=\"FluentSeparatorStyle\" TargetType=\"Separator\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentProgressBarStyle\" TargetType=\"ProgressBar\">",
            "<Style TargetType=\"{x:Type controls:NumberBox}\">",
            "<Setter Property=\"UseLayoutRounding\"   Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style TargetType=\"{x:Type controls:FluentExpander}\">",
            "</ResourceDictionary>",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"SetupSidebarNavItemStyle\" TargetType=\"RadioButton\">",
            "<Style x:Key=\"SetupSidebarNavTitleStyle\" TargetType=\"TextBlock\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            globalStyles,
            "<Style TargetType=\"DataGridColumnHeader\">",
            "<Style TargetType=\"DataGridCell\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            globalStyles,
            "<Style TargetType=\"DataGridCell\">",
            "<Style x:Key=\"PaginationButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            globalStyles,
            "<Style x:Key=\"SmartTooltipStyle\" TargetType=\"ToolTip\">",
            "<Style TargetType=\"controls:InlineTipBanner\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");
    }

    [Fact]
    public void SharedWindowChromeStyles_Should_Enable_Crisp_Layout_Settings()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentSeparatorStyle\" TargetType=\"Separator\">",
            "<Style x:Key=\"FluentMenuBarStyle\" TargetType=\"Menu\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentMenuSeparatorStyle\" TargetType=\"Separator\">",
            "<Style x:Key=\"FluentMenuBarItemStyle\" TargetType=\"MenuItem\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentStatusBarSeparatorStyle\" TargetType=\"Border\">",
            "<Style x:Key=\"FluentWindowStyle\" TargetType=\"Window\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentWindowStyle\" TargetType=\"Window\">",
            "<Style x:Key=\"FluentScrollBarThumb\" TargetType=\"Thumb\">",
            "<Setter Property=\"UseLayoutRounding\"    Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\"  Value=\"True\"/>",
            "<Setter Property=\"TextOptions.TextFormattingMode\" Value=\"Display\"/>",
            "<Setter Property=\"TextOptions.TextRenderingMode\"  Value=\"ClearType\"/>");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentScrollBarThumb\" TargetType=\"Thumb\">",
            "<Style x:Key=\"FluentVerticalScrollBar\" TargetType=\"ScrollBar\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentVerticalScrollBar\" TargetType=\"ScrollBar\">",
            "<Style x:Key=\"FluentHorizontalScrollBar\" TargetType=\"ScrollBar\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentHorizontalScrollBar\" TargetType=\"ScrollBar\">",
            "<Style x:Key=\"FluentScrollViewerStyle\" TargetType=\"ScrollViewer\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            fluentTheme,
            "<Style x:Key=\"FluentFocusVisualStyle\" TargetType=\"Control\">",
            "<Style x:Key=\"FluentRadioButtonStyle\" TargetType=\"RadioButton\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");
    }

    [Fact]
    public void PosSharedSurfaceStyles_Should_Enable_Crisp_Layout_Settings()
    {
        var posStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));

        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"SelectableUserButtonStyle\" TargetType=\"Button\">",
            "<Style x:Key=\"PosKeypadButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"PosKeypadButtonStyle\" TargetType=\"Button\">",
            "<Style x:Key=\"QuickActionButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"QuickActionButtonStyle\" TargetType=\"Button\">",
            "<Style x:Key=\"EnterpriseActionButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"EnterpriseActionButtonStyle\" TargetType=\"Button\">",
            "<Style x:Key=\"DangerActionButtonStyle\" TargetType=\"Button\"",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"StatusBadgePillStyle\" TargetType=\"Border\">",
            "<Style x:Key=\"ToolbarLinkButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>");

        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"ToolbarLinkButtonStyle\" TargetType=\"Button\">",
            "<Style x:Key=\"CartRemoveButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"CartRemoveButtonStyle\" TargetType=\"Button\">",
            "<Style x:Key=\"SegmentedButtonStyle\" TargetType=\"Button\">",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");

        AssertStyleBlockContains(
            posStyles,
            "<Style x:Key=\"SegmentedButtonStyle\" TargetType=\"Button\">",
            "</ResourceDictionary>",
            "<Setter Property=\"UseLayoutRounding\" Value=\"True\"/>",
            "<Setter Property=\"SnapsToDevicePixels\" Value=\"True\"/>",
            "UseLayoutRounding=\"True\"",
            "SnapsToDevicePixels=\"True\"");
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

    private static void AssertStyleBlockContains(
        string content,
        string startMarker,
        string endMarker,
        params string[] expectedSnippets)
    {
        var block = SliceBetween(content, startMarker, endMarker);

        foreach (var snippet in expectedSnippets)
        {
            Assert.Contains(snippet, block, StringComparison.Ordinal);
        }
    }

    private static string SliceBetween(string content, string startMarker, string endMarker)
    {
        var startIndex = content.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(startIndex >= 0, $"Could not find start marker '{startMarker}'.");

        var endIndex = content.IndexOf(endMarker, startIndex + startMarker.Length, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            endIndex = content.Length;
        }

        return content[startIndex..endIndex];
    }
}
