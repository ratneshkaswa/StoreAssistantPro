namespace StoreAssistantPro.Tests.Helpers;

public sealed class ExperiencePreferencesStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SystemSettings_Should_Expose_Experience_And_Notification_Preferences()
    {
        var view = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Settings", "Views", "SystemSettingsView.xaml"));
        var viewModel = File.ReadAllText(Path.Combine(SolutionRoot, "Modules", "Settings", "ViewModels", "SystemSettingsViewModel.cs"));

        Assert.Contains("Experience &amp; Notifications", view, StringComparison.Ordinal);
        Assert.Contains("RestoreLastVisitedPageOnLogin", view, StringComparison.Ordinal);
        Assert.Contains("InAppToastsEnabled", view, StringComparison.Ordinal);
        Assert.Contains("WindowsNotificationsEnabled", view, StringComparison.Ordinal);
        Assert.Contains("NotificationSoundEnabled", view, StringComparison.Ordinal);
        Assert.Contains("MinimumNotificationLevel", view, StringComparison.Ordinal);

        Assert.Contains("WorkspaceRestoreSummaryText", viewModel, StringComparison.Ordinal);
        Assert.Contains("NotificationPreferencesSummaryText", viewModel, StringComparison.Ordinal);
        Assert.Contains("UserPreferencesStore.Update", viewModel, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not find solution root from " + AppContext.BaseDirectory);
    }
}
