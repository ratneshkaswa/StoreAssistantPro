namespace StoreAssistantPro.Tests.Helpers;

public sealed class NotificationBadgeBehaviorStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void NotificationBadgeBehavior_Should_Use_Badge_Bounce_For_Count_Updates()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "NotificationBadgeBehavior.cs"));

        Assert.Contains("PlayBadgeBounce", source, StringComparison.Ordinal);
        Assert.Contains("1.0 → 1.2 → 1.0", source, StringComparison.Ordinal);
        Assert.Contains("if (wasHidden)", source, StringComparison.Ordinal);
        Assert.Contains("PlayBadgeEntrance(badge);", source, StringComparison.Ordinal);
        Assert.Contains("PlayBadgeBounce(badge);", source, StringComparison.Ordinal);
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
