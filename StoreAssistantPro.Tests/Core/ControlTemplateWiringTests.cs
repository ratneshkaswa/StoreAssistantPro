namespace StoreAssistantPro.Tests.Core;

public sealed class ControlTemplateWiringTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void InfoBar_Should_Detach_Previous_CloseButton_Handler_Before_Rewiring()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "InfoBar.cs"));

        Assert.Contains("_closeButton.Click -= OnCloseButtonClick;", content, StringComparison.Ordinal);
        Assert.Contains("_closeButton = GetTemplateChild(\"PART_CloseButton\") as Button;", content, StringComparison.Ordinal);
        Assert.Contains("_closeButton.Click += OnCloseButtonClick;", content, StringComparison.Ordinal);
    }

    [Fact]
    public void BreadcrumbBarItem_Should_Detach_Previous_Button_Handler_Before_Rewiring()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "BreadcrumbBar.cs"));

        Assert.Contains("_button.Click -= OnPartButtonClick;", content, StringComparison.Ordinal);
        Assert.Contains("_button = GetTemplateChild(\"PART_Button\") as Button;", content, StringComparison.Ordinal);
        Assert.Contains("_button.Click += OnPartButtonClick;", content, StringComparison.Ordinal);
    }

    [Fact]
    public void FluentExpander_Should_Detach_Previous_ToggleButton_Handler_Before_Rewiring()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "FluentExpander.cs"));

        Assert.Contains("_toggleButton.Click -= OnToggleButtonClick;", content, StringComparison.Ordinal);
        Assert.Contains("_toggleButton = GetTemplateChild(\"PART_ToggleButton\") as Button;", content, StringComparison.Ordinal);
        Assert.Contains("_toggleButton.Click += OnToggleButtonClick;", content, StringComparison.Ordinal);
    }

    [Fact]
    public void FluentExpander_Should_Wire_ContentArea_And_Apply_Immediate_State()
    {
        var code = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "FluentExpander.cs"));
        var style = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("_contentArea = GetTemplateChild(\"PART_ContentArea\") as Border;", code, StringComparison.Ordinal);
        Assert.Contains("UpdateExpandedState();", code, StringComparison.Ordinal);
        Assert.DoesNotContain("AnimateExpand();", code, StringComparison.Ordinal);
        Assert.DoesNotContain("AnimateCollapse();", code, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginAnimation(HeightProperty", code, StringComparison.Ordinal);
        Assert.Contains("Height = double.NaN;", code, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PART_ContentArea\"", style, StringComparison.Ordinal);
        Assert.Contains("Height=\"0\"", style, StringComparison.Ordinal);
        Assert.Contains("ClipToBounds=\"True\"", style, StringComparison.Ordinal);
    }

    [Fact]
    public void NumberBox_Should_Detach_Previous_TemplatePart_Handlers_Before_Rewiring()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "NumberBox.cs"));

        Assert.Contains("_textBox.PreviewTextInput -= OnPreviewTextInput;", content, StringComparison.Ordinal);
        Assert.Contains("_textBox.LostFocus -= OnTextBoxLostFocus;", content, StringComparison.Ordinal);
        Assert.Contains("_textBox.PreviewKeyDown -= OnTextBoxPreviewKeyDown;", content, StringComparison.Ordinal);
        Assert.Contains("DataObject.RemovePastingHandler(_textBox, OnPaste);", content, StringComparison.Ordinal);
        Assert.Contains("_upButton.Click -= OnUpButtonClick;", content, StringComparison.Ordinal);
        Assert.Contains("_downButton.Click -= OnDownButtonClick;", content, StringComparison.Ordinal);
        Assert.Contains("_upButton = GetTemplateChild(\"PART_UpButton\") as Button;", content, StringComparison.Ordinal);
        Assert.Contains("_downButton = GetTemplateChild(\"PART_DownButton\") as Button;", content, StringComparison.Ordinal);
        Assert.Contains("_upButton.Click += OnUpButtonClick;", content, StringComparison.Ordinal);
        Assert.Contains("_downButton.Click += OnDownButtonClick;", content, StringComparison.Ordinal);
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
