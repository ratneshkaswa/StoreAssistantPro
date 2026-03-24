namespace StoreAssistantPro.Tests.Helpers;

public sealed class RevealFocusStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void Shared_Focus_Visual_Should_Include_Reveal_Glow()
    {
        var themeSource = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("x:Key=\"FluentFocusVisualStyle\"", themeSource, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FocusGlow\"", themeSource, StringComparison.Ordinal);
        Assert.Contains("Background=\"{StaticResource FluentAccentDefault}\"", themeSource, StringComparison.Ordinal);
        Assert.Contains("BlurEffect Radius=\"10\"", themeSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_DataGrid_Cell_Focus_Should_Use_Reveal_Glow()
    {
        var styleSource = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("x:Name=\"FocusGlow\"", styleSource, StringComparison.Ordinal);
        Assert.Contains("Setter TargetName=\"FocusGlow\" Property=\"Opacity\" Value=\"0.18\"", styleSource, StringComparison.Ordinal);
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
