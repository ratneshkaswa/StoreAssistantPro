using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Tests.Helpers;
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

    [Fact]
    public void ConfigureDialogWindow_AfterMainWindowClosed_Should_NotReuseClosedOwner()
    {
        RunOnStaThread(() =>
        {
            var service = new WindowSizingService();
            var mainWindow = new TestWindow();
            PrepareWindow(mainWindow);

            service.ConfigureMainWindow(mainWindow);
            mainWindow.Show();
            mainWindow.Close();

            var dialog = new TestWindow();
            service.ConfigureDialogWindow(dialog, 640, 480);

            Assert.Null(dialog.Owner);
            Assert.Equal(WindowStartupLocation.CenterScreen, dialog.WindowStartupLocation);
        });
    }

    [Fact]
    public void ConfigureMainWindow_WhenNewMainWindowConfigured_Should_ReplaceStoredOwner()
    {
        RunOnStaThread(() =>
        {
            var service = new WindowSizingService();
            var oldMainWindow = new TestWindow();
            PrepareWindow(oldMainWindow);
            service.ConfigureMainWindow(oldMainWindow);
            oldMainWindow.Show();
            oldMainWindow.Close();

            var newMainWindow = new TestWindow();
            service.ConfigureMainWindow(newMainWindow);

            Assert.Same(newMainWindow, GetStoredMainWindow(service));
        });
    }

    private static void RunOnStaThread(Action action)
        => WpfTestApplication.Run(() =>
        {
            EnsureApplicationResources();
            action();
        });

    private static void EnsureApplicationResources()
        => WpfTestApplication.EnsureStoreAssistantApplication();

    private static void PrepareWindow(Window window)
    {
        window.Width = 200;
        window.Height = 120;
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.WindowStyle = WindowStyle.None;
        window.Left = -20000;
        window.Top = -20000;
        window.Opacity = 0;
    }

    private static Window? GetStoredMainWindow(WindowSizingService service)
    {
        var field = typeof(WindowSizingService).GetField("_mainWindow", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate WindowSizingService._mainWindow.");

        return field.GetValue(service) as Window;
    }

    private sealed class TestWindow : Window;
}
