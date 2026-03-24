using System.Windows.Controls;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class HelpHintTests
{
    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (caught is not null)
            throw new AggregateException(caught);
    }

    [Fact]
    public void HelpText_BridgesToSmartTooltip_AndEnablesCalloutStyle()
    {
        RunOnSta(() =>
        {
            var button = new Button();

            HelpHint.SetShortcutText(button, "Ctrl+K");
            HelpHint.SetHelpText(button, "Open the command palette.");

            Assert.Equal("Open the command palette.", SmartTooltip.GetText(button));
            Assert.Equal("Ctrl+K", SmartTooltip.GetHeader(button));
            Assert.True(SmartTooltip.GetUseCalloutStyle(button));
        });
    }

    [Fact]
    public void ClearingHelpText_RemovesCalloutStyle()
    {
        RunOnSta(() =>
        {
            var button = new Button();

            HelpHint.SetHelpText(button, "Temporary tip");
            Assert.True(SmartTooltip.GetUseCalloutStyle(button));

            HelpHint.SetHelpText(button, null);

            Assert.Null(SmartTooltip.GetText(button));
            Assert.False(SmartTooltip.GetUseCalloutStyle(button));
        });
    }
}
