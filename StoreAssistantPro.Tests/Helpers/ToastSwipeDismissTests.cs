using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class ToastSwipeDismissTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Theory]
    [InlineData(95, false)]
    [InlineData(96, true)]
    [InlineData(-96, true)]
    [InlineData(140, true)]
    public void ShouldDismiss_Uses_Horizontal_Threshold(double delta, bool expected)
    {
        Assert.Equal(expected, ToastSwipeDismiss.ShouldDismiss(delta));
    }

    [Theory]
    [InlineData(0, 1.0)]
    [InlineData(90, 0.675)]
    [InlineData(180, 0.35)]
    [InlineData(280, 0.35)]
    public void CalculateOpacity_Clamps_To_A_Useful_Drag_Range(double delta, double expected)
    {
        Assert.Equal(expected, ToastSwipeDismiss.CalculateOpacity(delta), 3);
    }

    [Fact]
    public void FluentTheme_Should_Enable_ToastSwipeDismiss_On_ToastRoot()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "FluentTheme.xaml"));

        Assert.Contains("h:ToastSwipeDismiss.IsEnabled=\"True\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ToastSwipeDismiss_Should_Reset_And_Dismiss_Without_Storyboard_Animation()
    {
        var source = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Helpers", "ToastSwipeDismiss.cs"));

        Assert.DoesNotContain("DoubleAnimation", source, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginAnimation", source, StringComparison.Ordinal);
        Assert.Contains("service.Dismiss(toast.Id);", source, StringComparison.Ordinal);
        Assert.Contains("translate.X = 0;", source, StringComparison.Ordinal);
        Assert.Contains("element.Opacity = 1;", source, StringComparison.Ordinal);
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
