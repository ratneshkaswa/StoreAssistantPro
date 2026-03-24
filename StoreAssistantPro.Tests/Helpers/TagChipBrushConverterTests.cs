using System.Globalization;
using System.Windows.Media;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public sealed class TagChipBrushConverterTests
{
    private readonly TagChipBrushConverter _converter = new();

    [Theory]
    [InlineData("Cash", "#0F7B0F")]
    [InlineData("UPI", "#0B57A4")]
    [InlineData("Credit", "#9A3412")]
    [InlineData("Card", "#6D28D9")]
    public void Known_Tags_Should_Map_To_Expected_Foreground(string value, string expectedHex)
    {
        var brush = Assert.IsType<SolidColorBrush>(
            _converter.Convert(value, typeof(Brush), "Foreground", CultureInfo.InvariantCulture));

        Assert.Equal((Color)ColorConverter.ConvertFromString(expectedHex)!, brush.Color);
    }

    [Fact]
    public void Blank_Tag_Should_Use_Neutral_Background()
    {
        var brush = Assert.IsType<SolidColorBrush>(
            _converter.Convert(" ", typeof(Brush), "Background", CultureInfo.InvariantCulture));

        Assert.Equal((Color)ColorConverter.ConvertFromString("#F3F4F6")!, brush.Color);
    }

    [Fact]
    public void Unknown_Tag_Should_Map_Consistently()
    {
        var first = Assert.IsType<SolidColorBrush>(
            _converter.Convert("Household", typeof(Brush), "Border", CultureInfo.InvariantCulture));
        var second = Assert.IsType<SolidColorBrush>(
            _converter.Convert("Household", typeof(Brush), "Border", CultureInfo.InvariantCulture));

        Assert.Equal(first.Color, second.Color);
    }
}
