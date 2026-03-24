using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
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
