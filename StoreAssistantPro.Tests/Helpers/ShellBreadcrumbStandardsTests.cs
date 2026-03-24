using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class ShellBreadcrumbStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void MainShell_Should_Expose_Breadcrumb_Path_For_Current_Page()
    {
        var viewModel = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "ViewModels", "MainViewModel.cs"));
        var window = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("ShellBreadcrumbItems", viewModel, StringComparison.Ordinal);
        Assert.Contains("UpdateShellBreadcrumbs()", viewModel, StringComparison.Ordinal);
        Assert.Contains("GetPageBreadcrumbItems", viewModel, StringComparison.Ordinal);
        Assert.Contains("\"Home\", \"Sales\"", viewModel, StringComparison.Ordinal);
        Assert.Contains("\"Home\", \"Inventory\"", viewModel, StringComparison.Ordinal);
        Assert.Contains("<controls:BreadcrumbBar", window, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding ShellBreadcrumbItems}\"", window, StringComparison.Ordinal);
        Assert.Contains("ItemClickCommand=\"{Binding ActivateBreadcrumbCommand}\"", window, StringComparison.Ordinal);
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
