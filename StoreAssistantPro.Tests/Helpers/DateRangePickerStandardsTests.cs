using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class DateRangePickerStandardsTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    [Fact]
    public void SharedStyles_Should_Define_DateRangePicker_Contract()
    {
        var control = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Controls", "DateRangePicker.cs"));
        var styles = File.ReadAllText(
            Path.Combine(SolutionRoot, "Core", "Styles", "GlobalStyles.xaml"));

        Assert.Contains("class DateRangePicker", control, StringComparison.Ordinal);
        Assert.Contains("StartDateProperty", control, StringComparison.Ordinal);
        Assert.Contains("EndDateProperty", control, StringComparison.Ordinal);
        Assert.Contains("IsDropDownOpenProperty", control, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"controls:DateRangePicker\">", styles, StringComparison.Ordinal);
        Assert.Contains("AnchoredFlyoutPopupStyle", styles, StringComparison.Ordinal);
        Assert.Contains("FluentCalendarStyle", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsView_Should_Use_Shared_DateRangePicker()
    {
        var reportsView = File.ReadAllText(
            Path.Combine(SolutionRoot, "Modules", "Reports", "Views", "ReportsView.xaml"));

        Assert.Contains("<controls:DateRangePicker", reportsView, StringComparison.Ordinal);
        Assert.Contains("StartDate=\"{Binding DateFrom, Mode=TwoWay}\"", reportsView, StringComparison.Ordinal);
        Assert.Contains("EndDate=\"{Binding DateTo, Mode=TwoWay}\"", reportsView, StringComparison.Ordinal);
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
