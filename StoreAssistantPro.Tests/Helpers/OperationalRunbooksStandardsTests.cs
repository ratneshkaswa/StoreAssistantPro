namespace StoreAssistantPro.Tests.Helpers;

public sealed class OperationalRunbooksStandardsTests
{
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void OperationsRunbooks_Should_Exist()
    {
        var files = new[]
        {
            @"docs\operations\README.md",
            @"docs\operations\BACKUP_AND_RESTORE_RUNBOOK.md",
            @"docs\operations\BILLING_RECOVERY_RUNBOOK.md",
            @"docs\operations\PRINTER_SETUP_RUNBOOK.md",
            @"docs\operations\RELEASE_VALIDATION_CHECKLIST.md",
            @"docs\operations\AUTHORIZATION_AND_DESTRUCTIVE_ACTION_AUDIT.md"
        };

        foreach (var relativePath in files)
        {
            Assert.True(File.Exists(Path.Combine(RepoRoot, relativePath)), $"Missing operations file: {relativePath}");
        }
    }

    [Fact]
    public void ReleaseScripts_AndPublishProfile_Should_Exist()
    {
        var files = new[]
        {
            @"scripts\release-readiness.ps1",
            @"scripts\performance-validation.ps1",
            @"scripts\publish-release.ps1",
            @"scripts\export-support-bundle.ps1",
            @"scripts\disaster-recovery-drill.ps1",
            @"Properties\PublishProfiles\StoreAssistantPro.Release.win-x64.pubxml"
        };

        foreach (var relativePath in files)
        {
            Assert.True(File.Exists(Path.Combine(RepoRoot, relativePath)), $"Missing release asset: {relativePath}");
        }
    }

    [Fact]
    public void ReleaseChecklist_Should_Reference_CoreValidationScripts()
    {
        var checklistPath = Path.Combine(RepoRoot, @"docs\operations\RELEASE_VALIDATION_CHECKLIST.md");
        var content = File.ReadAllText(checklistPath);

        Assert.Contains("release-readiness.ps1", content, StringComparison.Ordinal);
        Assert.Contains("performance-validation.ps1", content, StringComparison.Ordinal);
        Assert.Contains("publish-release.ps1", content, StringComparison.Ordinal);
        Assert.Contains("disaster-recovery-drill.ps1", content, StringComparison.Ordinal);
    }
}
