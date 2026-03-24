namespace StoreAssistantPro.Tests.Helpers;

public sealed class FilterChipBarStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Filter_Chip_Style_Should_Exist_In_PosStyles()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "PosStyles.xaml"));

        Assert.Contains("x:Key=\"ActiveFilterChipButtonStyle\"", source, StringComparison.Ordinal);
        Assert.Contains("Text=\"&#xE711;\"", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Modules\\Branch\\Views\\BranchManagementView.xaml", "Content=\"{Binding ActiveStatusFilter, StringFormat=Status: {0}}\"")]
    [InlineData("Modules\\Debtors\\Views\\DebtorManagementView.xaml", "Content=\"{Binding ActiveStatusFilter, StringFormat=Status: {0}}\"")]
    [InlineData("Modules\\Expenses\\Views\\ExpenseManagementView.xaml", "Content=\"{Binding ActiveDateFilter, StringFormat=Date: {0}}\"")]
    [InlineData("Modules\\Ironing\\Views\\IroningManagementView.xaml", "Content=\"{Binding ActivePaidFilter, StringFormat=Payment: {0}}\"")]
    [InlineData("Modules\\Reports\\Views\\ReportsView.xaml", "Content=\"{Binding ActivePreset, StringFormat=Preset: {0}}\"")]
    public void Filter_Heavy_Views_Should_Surface_Dismissable_Active_Filter_Chips(string relativePath, string bindingSnippet)
    {
        var source = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains("ActiveFilterChipButtonStyle", source, StringComparison.Ordinal);
        Assert.Contains(bindingSnippet, source, StringComparison.Ordinal);
        Assert.Contains("CommandParameter=", source, StringComparison.Ordinal);
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
