namespace StoreAssistantPro.Tests.Helpers;

public class PasswordRevealComplianceTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedPasswordBoxStyle_EnablesPasswordReveal()
    {
        var globalStyles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Setter Property=\"h:PasswordReveal.IsEnabled\" Value=\"True\"/>",
            globalStyles, StringComparison.Ordinal);
    }

    [Fact]
    public void FluentPasswordBoxTemplate_DefinesRevealButtonAndActiveTrigger()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("x:Name=\"PART_RevealButton\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("Property=\"h:PasswordReveal.IsRevealActive\"", fluentTheme, StringComparison.Ordinal);
        Assert.Contains("Path=(h:PasswordReveal.RevealText)", fluentTheme, StringComparison.Ordinal);
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
