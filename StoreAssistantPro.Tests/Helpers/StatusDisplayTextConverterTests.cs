using System.Globalization;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Tests.Helpers;

public class StatusDisplayTextConverterTests
{
    private static readonly StatusDisplayTextConverter Converter = new();

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("Pending", "Pending")]
    [InlineData("PartialReceived", "Partial Received")]
    [InlineData("InProgress", "In Progress")]
    [InlineData("Already Spaced", "Already Spaced")]
    [InlineData("snake_case_status", "snake case status")]
    public void Convert_FormatsStatusText(object? value, string expected) =>
        Assert.Equal(expected, Converter.Convert(value, typeof(string), null, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_FormatsEnumValues() =>
        Assert.Equal(
            "Partial Received",
            Converter.Convert(PurchaseOrderStatus.PartialReceived, typeof(string), null, CultureInfo.InvariantCulture));
}
