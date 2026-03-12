using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class WindowShellStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SecondaryWindows_Should_Inherit_BaseDialogWindow()
    {
        var exceptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MainWindow.xaml",
            "LoginWindow.xaml",
            "SetupWindow.xaml"
        };

        var violations = Directory
            .EnumerateFiles(Path.Combine(SolutionRoot, "Modules"), "*Window.xaml", SearchOption.AllDirectories)
            .Where(path => !exceptions.Contains(Path.GetFileName(path)))
            .Where(path => !File.ReadAllText(path).Contains("<core:BaseDialogWindow", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(SolutionRoot, path))
            .OrderBy(path => path)
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Secondary windows must inherit BaseDialogWindow so the shared two-tone shell is applied.\n"
            + string.Join("\n", violations));
    }

    [Theory]
    [InlineData("Modules\\Authentication\\Views\\LoginWindow.xaml")]
    [InlineData("Modules\\Authentication\\Views\\SetupWindow.xaml")]
    public void StartupWindows_Should_Use_AppBackground_And_SurfaceCard(string relativePath)
    {
        var content = File.ReadAllText(Path.Combine(SolutionRoot, relativePath));

        Assert.Contains("AppBackgroundBrush", content, StringComparison.Ordinal);
        Assert.Contains("FluentSurface", content, StringComparison.Ordinal);
        Assert.Contains("FluentSurfaceStroke", content, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_Should_Use_AppShell_SurfacePattern()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains("Background=\"{StaticResource AppBackgroundBrush}\"", content, StringComparison.Ordinal);
        Assert.Contains("Background=\"{StaticResource FluentSurface}\"", content, StringComparison.Ordinal);
        Assert.Contains("BorderBrush=\"{StaticResource FluentSurfaceStroke}\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void FormRowLabelStyle_Should_Be_LeftAligned()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Setter Property=\"HorizontalAlignment\" Value=\"Left\"/>", content, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"TextAlignment\" Value=\"Left\"/>", content, StringComparison.Ordinal);
        Assert.DoesNotContain("<Setter Property=\"HorizontalAlignment\" Value=\"Right\"/>", content, StringComparison.Ordinal);
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
