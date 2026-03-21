using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class AutomationNameFallbackTests
{
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
}
