using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class WorkspaceFabStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Styles_Should_Define_A_Floating_Action_Button()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("x:Key=\"FloatingActionButtonStyle\"", source, StringComparison.Ordinal);
        Assert.Contains("CornerRadius=\"28\"", source, StringComparison.Ordinal);
        Assert.Contains("ElevationEffectFlyout", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Workspace_Should_Surface_Start_Billing_As_An_Overlay_Fab()
    {
        var xaml = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "WorkspaceView.xaml"));

        Assert.Contains("x:Name=\"StartBillingFab\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FloatingActionButtonStyle}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("VerticalAlignment=\"Bottom\"", xaml, StringComparison.Ordinal);
        Assert.Contains("OpenBillingCommand", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"StartBillingButton\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Workspace_CodeBehind_Should_Focus_The_Fab_On_Load()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "WorkspaceView.xaml.cs"));

        Assert.Contains("StartBillingFab.Visibility", source, StringComparison.Ordinal);
        Assert.Contains("StartBillingFab.Focus()", source, StringComparison.Ordinal);
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
