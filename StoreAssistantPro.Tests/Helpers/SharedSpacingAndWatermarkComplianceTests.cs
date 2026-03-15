using System.Text.RegularExpressions;
using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public class SharedSpacingAndWatermarkComplianceTests
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
    public void GlobalStyles_Should_Enable_Fallback_Watermarks_For_Shared_Inputs()
    {
        var stylesFile = Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml");
        var styles = File.ReadAllText(stylesFile);

        Assert.True(
            Regex.Matches(styles, "h:Watermark.UseFallbackText\" Value=\"True\"", RegexOptions.CultureInvariant).Count >= 4,
            "TextBox, PasswordBox, ComboBox, and DatePicker should enable fallback watermarks in shared styles.");
        Assert.Contains("<Setter Property=\"TextAlignment\" Value=\"Left\"/>", styles, StringComparison.Ordinal);
        Assert.True(
            Regex.Matches(styles, "<Setter Property=\"HorizontalContentAlignment\" Value=\"Left\"/>", RegexOptions.CultureInvariant).Count >= 3,
            "PasswordBox, ComboBox, and DatePicker should enforce left content alignment in shared styles.");
    }

    [Fact]
    public void WatermarkHelper_Should_Support_Shared_Input_Controls()
    {
        var helperFile = Path.Combine(SolutionRoot, "Core", "Helpers", "Watermark.cs");
        var helper = File.ReadAllText(helperFile);

        Assert.DoesNotContain("temporarily disabled app-wide", helper, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("UseFallbackText", helper, StringComparison.Ordinal);
        Assert.Contains("PasswordBox", helper, StringComparison.Ordinal);
        Assert.Contains("ComboBox", helper, StringComparison.Ordinal);
        Assert.Contains("DatePicker", helper, StringComparison.Ordinal);
        Assert.Contains("GetGridSiblingLabelText", helper, StringComparison.Ordinal);
        Assert.Contains("TryGetHostedContentBounds", helper, StringComparison.Ordinal);
        Assert.Contains("PART_ContentHost", helper, StringComparison.Ordinal);
        Assert.Contains("\"ContentSite\"", helper, StringComparison.Ordinal);
        Assert.Contains("GetTextAlignment", helper, StringComparison.Ordinal);
        Assert.Contains("DependencyPropertyDescriptor", helper, StringComparison.Ordinal);
        Assert.Contains("ComboBox.TextProperty", helper, StringComparison.Ordinal);
        Assert.Contains("Selector.SelectedItemProperty", helper, StringComparison.Ordinal);
        Assert.Contains("ItemsControl.ItemsSourceProperty", helper, StringComparison.Ordinal);
        Assert.Contains("DatePicker.TextProperty", helper, StringComparison.Ordinal);
        Assert.Contains("DatePicker.SelectedDateProperty", helper, StringComparison.Ordinal);
        Assert.Contains("HookComboBoxValueChanges", helper, StringComparison.Ordinal);
        Assert.Contains("OnComboBoxValueChanged", helper, StringComparison.Ordinal);
        Assert.Contains("HookDatePickerValueChanges", helper, StringComparison.Ordinal);
        Assert.Contains("HasComboBoxValue", helper, StringComparison.Ordinal);
        Assert.Contains("HasDatePickerValue", helper, StringComparison.Ordinal);
        Assert.Contains("RefreshAdorner", helper, StringComparison.Ordinal);
        Assert.Contains("DispatcherPriority.Loaded", helper, StringComparison.Ordinal);
        Assert.Contains("FormattedText", helper, StringComparison.Ordinal);
        Assert.Contains("drawingContext.DrawText", helper, StringComparison.Ordinal);
        Assert.Contains("UseLayoutRounding = true", helper, StringComparison.Ordinal);
        Assert.Contains("TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);", helper, StringComparison.Ordinal);
        Assert.Contains("TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);", helper, StringComparison.Ordinal);
        Assert.DoesNotContain("control.HorizontalContentAlignment switch", helper, StringComparison.Ordinal);
    }

    [Fact]
    public void WatermarkHelper_Should_Rehook_ComboBox_And_DatePicker_ValueTracking_After_Reload()
    {
        var helper = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Helpers", "Watermark.cs"));

        Assert.Contains("HookComboBoxValueChanges(comboBox);", helper, StringComparison.Ordinal);
        Assert.Contains("HookDatePickerValueChanges(datePicker);", helper, StringComparison.Ordinal);
        Assert.Contains("UnhookComboBoxValueChanges(comboBox);", helper, StringComparison.Ordinal);
        Assert.Contains("UnhookDatePickerValueChanges(datePicker);", helper, StringComparison.Ordinal);
        Assert.Contains("case ComboBox comboBox:", helper, StringComparison.Ordinal);
        Assert.Contains("case DatePicker datePicker:", helper, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignSystem_Should_Use_Roomier_Global_Spacing()
    {
        var tokensFile = Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml");
        var tokens = File.ReadAllText(tokensFile);

        Assert.Contains("<Thickness x:Key=\"DialogPadding\">32</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"CardPadding\">16</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"FieldGroupSpacing\">0,0,0,16</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"ItemSpacing\">0,0,0,12</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"FormColumnGap\">0,0,16,0</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"CardContentPadding\">16,12</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<Thickness x:Key=\"ControlPadding\">12,8</Thickness>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"ControlHeight\">44</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"ButtonHeight\">40</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"SetupButtonHeight\">40</sys:Double>", tokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"FieldWidthCompact\">104</sys:Double>", tokens, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchInputs_Should_Use_Explicit_Example_Watermarks()
    {
        var billing = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Billing", "Views", "BillingWindow.xaml"));
        var customers = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Customers", "Views", "CustomerManagementWindow.xaml"));
        var vendors = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Vendors", "Views", "VendorManagementWindow.xaml"));

        Assert.Contains("h:Watermark.Text=\"e.g. Black T-shirt\"", billing, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. 8901234567890\"", billing, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. Aarav or 9876543210\"", customers, StringComparison.Ordinal);
        Assert.Contains("h:Watermark.Text=\"e.g. Jaipur Textiles or 08ABCDE1234F1Z5\"", vendors, StringComparison.Ordinal);

        Assert.DoesNotContain("h:Watermark.Text=\"Search product name...\"", billing, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Search by name or phone…\"", customers, StringComparison.Ordinal);
        Assert.DoesNotContain("h:Watermark.Text=\"Search by name, GSTIN, city…\"", vendors, StringComparison.Ordinal);
    }
}
