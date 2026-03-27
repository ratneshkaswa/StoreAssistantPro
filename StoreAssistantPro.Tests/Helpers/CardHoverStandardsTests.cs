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

    [Fact]
    public void Shared_Card_Styles_Should_Require_OptIn_For_Hover_Lift()
    {
        var styles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("<Style x:Key=\"CardStyle\" TargetType=\"Border\">", styles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"h:CardHover.IsEnabled\" Value=\"False\"/>", styles, StringComparison.Ordinal);
        Assert.Contains("<Style x:Key=\"InteractiveCardStyle\" TargetType=\"Border\"", styles, StringComparison.Ordinal);
        Assert.Contains("BasedOn=\"{StaticResource CardStyle}\"", styles, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"h:CardHover.IsEnabled\" Value=\"True\"/>", styles, StringComparison.Ordinal);
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
