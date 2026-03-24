namespace StoreAssistantPro.Tests.Helpers;

public sealed class DataGridRowGripStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_DataGrid_Row_Template_Should_Define_Hover_Grip()
    {
        var source = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("x:Name=\"RowGrip\"", source, StringComparison.Ordinal);
        Assert.Contains("Ellipse Width=\"3\" Height=\"3\"", source, StringComparison.Ordinal);
        Assert.Contains("Setter TargetName=\"RowGrip\" Property=\"Opacity\" Value=\"1\"", source, StringComparison.Ordinal);
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
