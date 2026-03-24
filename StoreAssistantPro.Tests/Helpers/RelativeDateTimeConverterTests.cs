using System.Globalization;
using StoreAssistantPro.Core.Helpers;
using Xunit;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class RelativeDateTimeConverterTests
{
    private static readonly RelativeDateTimeConverter Converter = new();

    [Fact]
    public void Convert_Returns_JustNow_For_RecentTimestamp()
    {
        var value = Converter.Convert(
            DateTime.Now - TimeSpan.FromSeconds(20),
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal("Just now", value);
    }

    [Fact]
    public void Convert_Returns_MinutesAgo_For_RecentMinutes()
    {
        var value = Converter.Convert(
            DateTime.Now - TimeSpan.FromMinutes(5),
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal("5 min ago", value);
    }

    [Fact]
    public void Convert_Supports_DateTimeOffset_Input()
    {
        var value = Converter.Convert(
            new DateTimeOffset(DateTime.Now - TimeSpan.FromMinutes(5)),
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal("5 min ago", value);
    }

    [Fact]
    public void Convert_Returns_Yesterday_For_PreviousDay()
    {
        var yesterday = DateTime.Now.Date.AddDays(-1).AddHours(14);

        var value = Converter.Convert(
            yesterday,
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal("Yesterday", value);
    }

    [Fact]
    public void Convert_Returns_FullDate_For_OlderTimestamp()
    {
        var older = DateTime.Now.Date.AddDays(-5).AddHours(9).AddMinutes(30);

        var value = Converter.Convert(
            older,
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal(older.ToString("g", CultureInfo.InvariantCulture), value);
    }

    [Fact]
    public void Convert_Returns_FullDate_For_FutureTimestamp()
    {
        var future = DateTime.Now.AddHours(2);

        var value = Converter.Convert(
            future,
            typeof(string),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal(future.ToString("g", CultureInfo.InvariantCulture), value);
    }

    [Fact]
    public void Convert_Returns_FullParameter_Format_When_Requested()
    {
        var value = DateTime.Now.Date.AddHours(10).AddMinutes(15);

        var result = Converter.Convert(
            value,
            typeof(string),
            "Full",
            CultureInfo.InvariantCulture);

        Assert.Equal(value.ToString("f", CultureInfo.InvariantCulture), result);
    }

    [Fact]
    public void MainWindow_Should_Use_RelativeDateTimeConverter_For_NotificationTimestamps()
    {
        var solutionRoot = FindSolutionRoot();
        var appXaml = File.ReadAllText(Path.Combine(solutionRoot, "App.xaml"));
        var mainWindowXaml = File.ReadAllText(
            Path.Combine(solutionRoot, "Modules", "MainShell", "Views", "MainWindow.xaml"));

        Assert.Contains(
            "<helpers:RelativeDateTimeConverter x:Key=\"RelativeDateTimeConverter\"/>",
            appXaml,
            StringComparison.Ordinal);
        Assert.Contains(
            "Text=\"{Binding Timestamp, Converter={StaticResource RelativeDateTimeConverter}}\"",
            mainWindowXaml,
            StringComparison.Ordinal);
        Assert.Contains(
            "ToolTip=\"{Binding Timestamp, Converter={StaticResource RelativeDateTimeConverter}, ConverterParameter=Full}\"",
            mainWindowXaml,
            StringComparison.Ordinal);
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
