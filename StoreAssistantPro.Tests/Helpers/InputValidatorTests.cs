using StoreAssistantPro.Core.Helpers;

namespace StoreAssistantPro.Tests.Helpers;

public class InputValidatorTests
{
    // ── IsValidUserPin ──

    [Theory]
    [InlineData("1234", true)]
    [InlineData("0000", true)]
    [InlineData("123", false)]
    [InlineData("12345", false)]
    [InlineData("abcd", false)]
    [InlineData("12a4", false)]
    [InlineData("", false)]
    public void IsValidUserPin_ValidatesCorrectly(string pin, bool expected) =>
        Assert.Equal(expected, InputValidator.IsValidUserPin(pin));

    // ── IsValidMasterPin ──

    [Theory]
    [InlineData("123456", true)]
    [InlineData("000000", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("abcdef", false)]
    [InlineData("", false)]
    public void IsValidMasterPin_ValidatesCorrectly(string pin, bool expected) =>
        Assert.Equal(expected, InputValidator.IsValidMasterPin(pin));

    // ── IsRequired ──

    [Theory]
    [InlineData("hello", true)]
    [InlineData(" a ", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void IsRequired_ValidatesCorrectly(string? value, bool expected) =>
        Assert.Equal(expected, InputValidator.IsRequired(value));

    // ── IsNonNegative ──

    [Theory]
    [InlineData(0, true)]
    [InlineData(100.50, true)]
    [InlineData(-0.01, false)]
    [InlineData(-1, false)]
    public void IsNonNegative_ValidatesCorrectly(double value, bool expected) =>
        Assert.Equal(expected, InputValidator.IsNonNegative((decimal)value));

    // ── AreEqual ──

    [Fact]
    public void AreEqual_MatchingValues_ReturnsTrue() =>
        Assert.True(InputValidator.AreEqual("1234", "1234"));

    [Fact]
    public void AreEqual_DifferentValues_ReturnsFalse() =>
        Assert.False(InputValidator.AreEqual("1234", "5678"));

    // ── AreAllDistinct ──

    [Fact]
    public void AreAllDistinct_UniqueValues_ReturnsTrue() =>
        Assert.True(InputValidator.AreAllDistinct("1111", "2222", "3333"));

    [Fact]
    public void AreAllDistinct_DuplicateValues_ReturnsFalse() =>
        Assert.False(InputValidator.AreAllDistinct("1111", "2222", "1111"));

    [Fact]
    public void AreAllDistinct_AllSame_ReturnsFalse() =>
        Assert.False(InputValidator.AreAllDistinct("1111", "1111", "1111"));
}
