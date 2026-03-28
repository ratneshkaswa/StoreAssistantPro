using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class SmoothScrollComplianceTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SmoothScroll_Should_Retain_Attached_Property_But_Avoid_Custom_Wheel_Handling()
    {
        var content = File.ReadAllText(Path.Combine(
            SolutionRoot,
            "Core",
            "Helpers",
            "SmoothScroll.cs"));

        Assert.Contains("IsEnabledProperty", content, StringComparison.Ordinal);
        Assert.Contains("OnNoOpChanged", content, StringComparison.Ordinal);
        Assert.Contains("speed-first mode leaves scrolling fully native", content, StringComparison.Ordinal);
        Assert.DoesNotContain("PreviewMouseWheel += OnPreviewMouseWheel", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ScrollToVerticalOffset", content, StringComparison.Ordinal);
        Assert.DoesNotContain("SystemParameters.WheelScrollLines", content, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Views_Using_SmoothScroll_Should_Use_Consistent_ScrollViewer_Settings()
    {
        var windowFiles = Directory.EnumerateFiles(
            Path.Combine(SolutionRoot, "Modules"),
            "*Window.xaml",
            SearchOption.AllDirectories);

        var violations = new List<string>();

        foreach (var file in windowFiles)
        {
            var content = File.ReadAllText(file);
            var matches = System.Text.RegularExpressions.Regex.Matches(
                content,
                @"<ScrollViewer\b(?:(?!>).|\r|\n)*h:SmoothScroll\.IsEnabled=""True""(?:(?!>).|\r|\n)*>",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var block = match.Value;
                if (!block.Contains("Focusable=\"False\"", StringComparison.Ordinal) ||
                    !block.Contains("PanningMode=\"VerticalOnly\"", StringComparison.Ordinal) ||
                    block.Contains("CanContentScroll=\"True\"", StringComparison.Ordinal))
                {
                    violations.Add(Path.GetRelativePath(SolutionRoot, file));
                    break;
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "SmoothScroll windows must use Focusable=\"False\", PanningMode=\"VerticalOnly\", and must not set CanContentScroll=\"True\".\n"
            + string.Join("\n", violations.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(path => path)));
    }

    [Fact]
    public void Shared_Hosts_Should_Enable_SmoothScroll()
    {
        var appXaml = File.ReadAllText(Path.Combine(
            SolutionRoot,
            "App.xaml"));
        var baseDialogWindow = File.ReadAllText(Path.Combine(
            SolutionRoot,
            "Core",
            "Base",
            "BaseDialogWindow.cs"));

        Assert.Contains("helpers:SmoothScroll.IsEnabled=\"True\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("PanningMode=\"VerticalOnly\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("CanContentScroll=\"False\"", appXaml, StringComparison.Ordinal);
        Assert.Contains("SmoothScroll.SetIsEnabled(scrollViewer, true);", baseDialogWindow, StringComparison.Ordinal);
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
