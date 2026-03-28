namespace StoreAssistantPro.Tests.Helpers;

public sealed class AutoGrowTextBoxTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void AutoGrowTextBox_Should_Use_Static_Height_Updates()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "AutoGrowTextBox.cs"));

        Assert.DoesNotContain("DoubleAnimation", source, StringComparison.Ordinal);
        Assert.DoesNotContain("QuadraticEase", source, StringComparison.Ordinal);
        Assert.Contains("_textBox.SetCurrentValue(FrameworkElement.HeightProperty, targetHeight);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AutoGrowTextBox_Should_Guard_Against_Reentrant_Update_Queues()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "AutoGrowTextBox.cs"));

        Assert.Contains("private bool _updatePending;", source, StringComparison.Ordinal);
        Assert.Contains("private bool _isApplyingHeight;", source, StringComparison.Ordinal);
        Assert.Contains("if (_updatePending)", source, StringComparison.Ordinal);
        Assert.Contains("if (_isApplyingHeight)", source, StringComparison.Ordinal);
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
