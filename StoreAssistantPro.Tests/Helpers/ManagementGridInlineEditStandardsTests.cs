namespace StoreAssistantPro.Tests.Helpers;

public sealed class ManagementGridInlineEditStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_DataGrid_Inline_Edit_And_Selector_Surfaces_Should_Exist()
    {
        var helper = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Helpers", "DataGridMultiSelect.cs"));
        var globalStyles = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("DataGridSelectionMode.Extended", helper, StringComparison.Ordinal);
        Assert.Contains("DataGridRowSelectionCheckBoxStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("DataGridInlineEditTextBoxStyle", globalStyles, StringComparison.Ordinal);
        Assert.Contains("DataGridGroupItemStyle", globalStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void Brand_Management_Should_Use_Inline_Rename_And_Multi_Select()
    {
        var source = File.ReadAllText(Path.Combine(
            SolutionRoot, "Modules", "Brands", "Views", "BrandManagementView.xaml"));

        Assert.Contains("x:Name=\"BrandsGrid\"", source, StringComparison.Ordinal);
        Assert.Contains("h:DataGridMultiSelect.IsEnabled=\"True\"", source, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseDoubleClick=\"OnBrandsGridPreviewMouseDoubleClick\"", source, StringComparison.Ordinal);
        Assert.Contains("DataGridInlineEditTextBoxStyle", source, StringComparison.Ordinal);
        Assert.Contains("PencilIcon", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Category_Management_Should_Use_Grouped_Inline_Edit_Grid()
    {
        var source = File.ReadAllText(Path.Combine(
            SolutionRoot, "Modules", "Categories", "Views", "CategoryManagementView.xaml"));

        Assert.Contains("GroupedCategoriesView", source, StringComparison.Ordinal);
        Assert.Contains("PropertyGroupDescription PropertyName=\"CategoryType.Name\"", source, StringComparison.Ordinal);
        Assert.Contains("GroupStyle ContainerStyle=\"{StaticResource DataGridGroupItemStyle}\"", source, StringComparison.Ordinal);
        Assert.Contains("h:DataGridMultiSelect.IsEnabled=\"True\"", source, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseDoubleClick=\"OnCategoriesGridPreviewMouseDoubleClick\"", source, StringComparison.Ordinal);
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
