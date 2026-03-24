using System.Globalization;
using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class DoubleLessThanConverterTests
{
    private readonly DoubleLessThanConverter _sut = new();

    [Theory]
    [InlineData(1200d, "1400", true)]
    [InlineData(1400d, "1400", false)]
    [InlineData(1600d, "1400", false)]
    public void Convert_ReturnsExpectedComparison(double value, string threshold, bool expected)
    {
        var result = _sut.Convert(value, typeof(bool), threshold, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Convert_ReturnsFalse_WhenValueCannotBeParsed()
    {
        var result = _sut.Convert("not-a-number", typeof(bool), "1400", CultureInfo.InvariantCulture);

        Assert.False((bool)result);
    }
}
