using System.Windows;
using StoreAssistantPro.Core.Services;
using Xunit;

namespace StoreAssistantPro.Tests.Services;

public sealed class WindowSizingServiceTests
{
    [Fact]
    public void ClampToVisibleArea_Should_KeepWindowInsideWorkArea()
    {
        var result = WindowSizingService.ClampToVisibleArea(
            new Rect(1180, 650, 300, 200),
            new Rect(0, 0, 1280, 720));

        Assert.Equal(300, result.Width);
        Assert.Equal(200, result.Height);
        Assert.Equal(972, result.Left);
        Assert.Equal(512, result.Top);
    }

    [Fact]
    public void ClampToVisibleArea_Should_ShrinkOversizedWindowsBeforePositioning()
    {
        var result = WindowSizingService.ClampToVisibleArea(
            new Rect(-40, -20, 1600, 900),
            new Rect(0, 0, 1280, 720));

        Assert.Equal(1264, result.Width);
        Assert.Equal(704, result.Height);
        Assert.Equal(8, result.Left);
        Assert.Equal(8, result.Top);
    }
}
