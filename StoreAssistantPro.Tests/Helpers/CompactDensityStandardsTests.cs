namespace StoreAssistantPro.Tests.Helpers;

public sealed class CompactDensityStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void DesignSystem_Should_Define_Runtime_Density_Tokens()
    {
        var source = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Styles", "DesignSystem.xaml"));

        Assert.Contains("x:Key=\"NormalDensityDataGridRowHeight\"", source, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"CompactDensityDataGridRowHeight\"", source, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"AppDataGridRowHeight\"", source, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"AppDataGridCellPadding\"", source, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"AppDataGridHeaderPadding\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedGridStyles_Should_Use_Runtime_Density_Resources()
    {
        var source = File.ReadAllText(Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("Value=\"{DynamicResource AppDataGridRowHeight}\"", source, StringComparison.Ordinal);
        Assert.Contains("Value=\"{DynamicResource AppDataGridCellPadding}\"", source, StringComparison.Ordinal);
        Assert.Contains("Value=\"{DynamicResource AppDataGridHeaderPadding}\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SystemSettings_Should_Expose_Density_Toggle()
    {
        var source = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));

        Assert.Contains("SetDensityModeCommand", source, StringComparison.Ordinal);
        Assert.Contains("Content=\"Compact\"", source, StringComparison.Ordinal);
        Assert.Contains("Content=\"Normal\"", source, StringComparison.Ordinal);
        Assert.Contains("DensitySummaryText", source, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not find solution root from " + AppContext.BaseDirectory);
    }
}
