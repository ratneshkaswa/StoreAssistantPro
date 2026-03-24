using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class CardHoverStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void CardHover_Should_Set_Hand_Cursor_For_Clickable_Cards()
    {
        var content = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "CardHover.cs"));

        Assert.Contains("border.Cursor = Cursors.Hand;", content, StringComparison.Ordinal);
        Assert.Contains("border.ClearValue(FrameworkElement.CursorProperty);", content, StringComparison.Ordinal);
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
