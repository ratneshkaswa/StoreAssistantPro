namespace StoreAssistantPro.Tests.Helpers;

public sealed class WindowsNotificationBridgeStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void App_And_Hosting_Should_Register_And_Activate_WindowsNotificationBridge()
    {
        var appCode = File.ReadAllText(
            Path.Combine(SolutionRoot, "App.xaml.cs"));
        var hosting = File.ReadAllText(
            Path.Combine(SolutionRoot, "HostingExtensions.cs"));

        Assert.Contains("GetRequiredService<WindowsNotificationBridge>()", appCode, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<IWindowsNotificationPresenter, WindowsToastNotificationPresenter>()", hosting, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<WindowsNotificationBridge>()", hosting, StringComparison.Ordinal);
    }

    [Fact]
    public void WindowsToastPresenter_Should_Register_Desktop_AppId_And_Show_Native_Toasts()
    {
        var presenter = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Services", "WindowsToastNotificationPresenter.cs"));
        var launchArgs = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Services", "WindowsNotificationLaunchArguments.cs"));

        Assert.Contains("StoreAssistantPro.Desktop", presenter, StringComparison.Ordinal);
        Assert.Contains("SetCurrentProcessExplicitAppUserModelID", presenter, StringComparison.Ordinal);
        Assert.Contains("powershell.exe", presenter, StringComparison.Ordinal);
        Assert.Contains("ToastNotificationManager", presenter, StringComparison.Ordinal);
        Assert.Contains("CreateToastNotifier", presenter, StringComparison.Ordinal);
        Assert.Contains("IShellLinkW", presenter, StringComparison.Ordinal);
        Assert.Contains("WindowsNotificationLaunchArguments.Build(notification)", presenter, StringComparison.Ordinal);
        Assert.Contains("notificationId", launchArgs, StringComparison.Ordinal);
        Assert.Contains("pageKey", launchArgs, StringComparison.Ordinal);
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
