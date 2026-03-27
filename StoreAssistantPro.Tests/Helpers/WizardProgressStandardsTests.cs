using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class WizardProgressStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void InwardEntry_Should_Use_Segmented_Wizard_Progress_Bar()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Inward", "Views", "InwardEntryView.xaml"));

        Assert.Contains("x:Name=\"Step1Segment\"", source, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"Step2Segment\"", source, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"Step3Segment\"", source, StringComparison.Ordinal);
        Assert.Contains("ChromeAltFillColorSecondary", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RepeatBehavior=\"Forever\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PulseStoryboard", source, StringComparison.Ordinal);
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
