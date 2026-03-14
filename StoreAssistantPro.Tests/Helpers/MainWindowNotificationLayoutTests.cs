using StoreAssistantPro.Modules.MainShell.Views;
using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class MainWindowNotificationLayoutTests
{
    [Fact]
    public void CalculateNotificationPanelWidth_Should_ClampToAvailableShellWidth()
    {
        var result = MainWindow.CalculateNotificationPanelWidth(
            preferredWidth: 320,
            availableWidth: 248,
            anchorWidth: 36);

        Assert.Equal(248, result);
    }

    [Fact]
    public void CalculateNotificationPanelWidth_Should_NotShrinkBelowBellWidth()
    {
        var result = MainWindow.CalculateNotificationPanelWidth(
            preferredWidth: 320,
            availableWidth: 20,
            anchorWidth: 36);

        Assert.Equal(36, result);
    }

    [Fact]
    public void CalculateNotificationPopupOffset_Should_RightAlignPanelToBell()
    {
        var result = MainWindow.CalculateNotificationPopupOffset(
            panelWidth: 248,
            anchorWidth: 36);

        Assert.Equal(-212, result);
    }

    [Fact]
    public void CalculateNotificationPanelMaxHeight_Should_ClampToVisibleHeight()
    {
        var result = MainWindow.CalculateNotificationPanelMaxHeight(
            preferredMaxHeight: 400,
            availableHeight: 260);

        Assert.Equal(260, result);
    }
}
