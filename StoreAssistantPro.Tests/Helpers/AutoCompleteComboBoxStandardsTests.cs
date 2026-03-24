using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class AutoCompleteComboBoxStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedTheme_Should_Define_AutoSuggest_ComboBox_Style_And_Helper()
    {
        var helper = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "AutoCompleteComboBox.cs"));
        var theme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("class AutoCompleteComboBox", helper, StringComparison.Ordinal);
        Assert.Contains("PART_EditableTextBox", helper, StringComparison.Ordinal);
        Assert.Contains("_comboBox.Items.Filter", helper, StringComparison.Ordinal);
        Assert.Contains("Contains(query, StringComparison.CurrentCultureIgnoreCase)", helper, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"FluentAutoSuggestComboBoxStyle\"", theme, StringComparison.Ordinal);
        Assert.Contains("h:AutoCompleteComboBox.IsEnabled", theme, StringComparison.Ordinal);
    }

    [Fact]
    public void LargePickList_Pages_Should_Use_AutoSuggest_ComboBox_Style()
    {
        var grn = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "GRN", "Views", "GRNManagementView.xaml"));
        var quotations = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Quotations", "Views", "QuotationManagementView.xaml"));
        var purchaseOrders = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "PurchaseOrders", "Views", "PurchaseOrderView.xaml"));
        var inward = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));

        Assert.Contains("Style=\"{StaticResource FluentAutoSuggestComboBoxStyle}\"", grn, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FluentAutoSuggestComboBoxStyle}\"", quotations, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FluentAutoSuggestComboBoxStyle}\"", purchaseOrders, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FluentAutoSuggestComboBoxStyle}\"", inward, StringComparison.Ordinal);
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
