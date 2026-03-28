namespace StoreAssistantPro.Tests.Helpers;

public sealed class NotificationBadgeBehaviorStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void NotificationBadgeBehavior_Should_Update_Quietly_Without_Badge_Bounce()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "NotificationBadgeBehavior.cs"));

        Assert.Contains("UpdateBadge(badge, newCount, panel);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayBellPulse", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayBadgeEntrance", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PlayBadgeBounce", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DoubleAnimation", source, StringComparison.Ordinal);
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
