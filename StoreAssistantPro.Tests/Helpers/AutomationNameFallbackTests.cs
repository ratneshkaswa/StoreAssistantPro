using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class AutomationNameFallbackTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (caught is not null)
            throw new AggregateException(caught);
    }

    [Fact]
    public void ToolTip_WithoutExplicitName_SetsAutomationName()
    {
        RunOnSta(() =>
        {
            var button = new Button { ToolTip = "Refresh data" };

            AutomationNameFallback.SetUseToolTipFallback(button, true);

            Assert.Equal("Refresh data", AutomationProperties.GetName(button));
        });
    }

    [Fact]
    public void ExplicitName_IsPreserved()
    {
        RunOnSta(() =>
        {
            var button = new Button { ToolTip = "Delete row" };
            AutomationProperties.SetName(button, "Delete selected customer");

            AutomationNameFallback.SetUseToolTipFallback(button, true);

            Assert.Equal("Delete selected customer", AutomationProperties.GetName(button));
        });
    }

    [Fact]
    public void FallbackName_TracksToolTipChanges()
    {
        RunOnSta(() =>
        {
            var button = new Button { ToolTip = "Open details" };
            AutomationNameFallback.SetUseToolTipFallback(button, true);

            button.ToolTip = "Open invoice details";

            Assert.Equal("Open invoice details", AutomationProperties.GetName(button));
        });
    }

    [Fact]
    public void ToolTipFallback_Works_For_MenuItem()
    {
        RunOnSta(() =>
        {
            var menuItem = new MenuItem { ToolTip = "Open reports" };

            AutomationNameFallback.SetUseToolTipFallback(menuItem, true);

            Assert.Equal("Open reports", AutomationProperties.GetName(menuItem));
        });
    }

    [Fact]
    public void Shared_Menu_Styles_Should_Enable_ToolTip_Fallback()
    {
        var fluentTheme = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains(
            "<Style x:Key=\"FluentMenuItemStyle\" TargetType=\"MenuItem\">",
            fluentTheme,
            StringComparison.Ordinal);
        Assert.Contains(
            "<Style x:Key=\"FluentMenuBarItemStyle\" TargetType=\"MenuItem\">",
            fluentTheme,
            StringComparison.Ordinal);
        Assert.Contains(
            "<Style x:Key=\"FluentContextMenuItemStyle\" TargetType=\"MenuItem\">",
            fluentTheme,
            StringComparison.Ordinal);
        Assert.Contains(
            "<Setter Property=\"h:AutomationNameFallback.UseToolTipFallback\" Value=\"True\"/>",
            fluentTheme,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitIconOnlyButtons_Should_Expose_AccessibleNames_OrTooltips()
    {
        var violations = new List<string>();
        var glyphRegex = new Regex("Content=\"([^\"]+)\"", RegexOptions.Compiled);

        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(SolutionRoot, "Modules"),
                     "*.xaml",
                     SearchOption.AllDirectories)
                 .Concat(
                     Directory.EnumerateFiles(
                         Path.Combine(SolutionRoot, "Core"),
                         "*.xaml",
                         SearchOption.AllDirectories)))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains("<Button", StringComparison.Ordinal))
                    continue;

                var block = lines[i];
                for (var j = i + 1; j < Math.Min(i + 5, lines.Length); j++)
                {
                    block += " " + lines[j];
                    if (lines[j].Contains("/>", StringComparison.Ordinal) ||
                        lines[j].Contains(">", StringComparison.Ordinal))
                    {
                        break;
                    }
                }

                var match = glyphRegex.Match(block);
                if (!match.Success)
                    continue;

                var content = match.Groups[1].Value;
                var looksIconOnly =
                    Regex.IsMatch(content, "^&#x[0-9A-Fa-f]+;$") ||
                    content.Any(ch => !char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch));
                if (!looksIconOnly)
                    continue;

                if (block.Contains("AutomationProperties.Name=", StringComparison.Ordinal) ||
                    block.Contains("ToolTip=", StringComparison.Ordinal) ||
                    block.Contains("h:SmartTooltip.", StringComparison.Ordinal))
                {
                    continue;
                }

                violations.Add($"{Path.GetRelativePath(SolutionRoot, file)}:{i + 1}");
            }
        }

        Assert.True(
            violations.Count == 0,
            "Explicit icon-only buttons must expose an automation name directly or provide a tooltip fallback.\n"
            + string.Join("\n", violations));
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
