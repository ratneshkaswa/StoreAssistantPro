namespace StoreAssistantPro.Tests.Helpers;

public sealed class StartupMigrationStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void HostingExtensions_Should_Log_PendingModelChanges_Instead_Of_Throwing_During_Startup()
    {
        var hosting = File.ReadAllText(
            Path.Combine(SolutionRoot, "HostingExtensions.cs"));

        Assert.Contains("ConfigureWarnings", hosting, StringComparison.Ordinal);
        Assert.Contains("RelationalEventId.PendingModelChangesWarning", hosting, StringComparison.Ordinal);
        Assert.Contains("warnings.Log(RelationalEventId.PendingModelChangesWarning)", hosting, StringComparison.Ordinal);
    }

    [Fact]
    public void StartupService_Should_Not_Continue_With_Compatibility_Patches_If_Database_Was_Never_Created()
    {
        var startupService = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Startup", "Services", "StartupService.cs"));

        Assert.Contains("CanConnectSafelyAsync", startupService, StringComparison.Ordinal);
        Assert.Contains("Database creation was blocked by EF pending-model validation", startupService, StringComparison.Ordinal);
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
