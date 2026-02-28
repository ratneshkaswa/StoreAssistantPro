using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Intents;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Tests.Services;

public class IntentDetectionServiceTests
{
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();
    private readonly IIntentDetectionService _sut;

    public IntentDetectionServiceTests()
    {
        var perf = new PerformanceMonitor(NullLogger<PerformanceMonitor>.Instance);
        var logger = NullLogger<IntentDetectionService>.Instance;
        _sut = new IntentDetectionService(_eventBus, perf, logger);
    }

    // ── PIN detection ────────────────────────────────────────────────

    [Fact]
    public async Task Classify_4DigitPin_InPinContext_ReturnsPinCompleted()
    {
        var result = await _sut.ClassifyAsync("4829", InputContext.PinEntry);

        Assert.Equal(InputIntent.PinCompleted, result.Intent);
        Assert.Equal(1.0, result.Confidence);
        Assert.Equal("UserPin", result.ResolvedValue);
    }

    [Fact]
    public async Task Classify_6DigitPin_InPinContext_ReturnsMasterPin()
    {
        var result = await _sut.ClassifyAsync("829417", InputContext.PinEntry);

        Assert.Equal(InputIntent.PinCompleted, result.Intent);
        Assert.Equal(1.0, result.Confidence);
        Assert.Equal("MasterPin", result.ResolvedValue);
    }

    [Fact]
    public async Task Classify_3DigitPin_InPinContext_ReturnsUnknown()
    {
        var result = await _sut.ClassifyAsync("123", InputContext.PinEntry);

        Assert.Equal(InputIntent.Unknown, result.Intent);
    }

    [Fact]
    public async Task Classify_5DigitPin_InPinContext_ReturnsUnknown()
    {
        var result = await _sut.ClassifyAsync("12345", InputContext.PinEntry);

        Assert.Equal(InputIntent.Unknown, result.Intent);
    }

    [Fact]
    public async Task Classify_LettersInPinContext_ReturnsUnknown()
    {
        var result = await _sut.ClassifyAsync("abcd", InputContext.PinEntry);

        Assert.Equal(InputIntent.Unknown, result.Intent);
    }

    [Fact]
    public async Task Classify_4Digits_InGeneralContext_DoesNotReturnPin()
    {
        var result = await _sut.ClassifyAsync("4829", InputContext.General);

        Assert.NotEqual(InputIntent.PinCompleted, result.Intent);
    }

    // ── Barcode detection ────────────────────────────────────────────

    [Fact]
    public async Task Classify_Ean13_WithValidCheckDigit_HighConfidence()
    {
        // 4006381333931 is a valid EAN-13
        var result = await _sut.ClassifyAsync(
            "4006381333931", InputContext.BillingSearch);

        Assert.Equal(InputIntent.BarcodeScan, result.Intent);
        Assert.True(result.Confidence >= 0.9);
    }

    [Fact]
    public async Task Classify_8DigitBarcode_ReturnsBarcodeScan()
    {
        var result = await _sut.ClassifyAsync(
            "12345678", InputContext.BillingSearch);

        Assert.Equal(InputIntent.BarcodeScan, result.Intent);
        Assert.True(result.Confidence >= 0.7);
    }

    [Fact]
    public async Task Classify_12DigitUpc_ReturnsBarcodeScan()
    {
        var result = await _sut.ClassifyAsync(
            "012345678905", InputContext.ProductSearch);

        Assert.Equal(InputIntent.BarcodeScan, result.Intent);
        Assert.True(result.Confidence >= 0.7);
    }

    [Fact]
    public async Task Classify_RapidEntry_BoostsConfidence()
    {
        var resultSlow = await _sut.ClassifyAsync(
            "12345678", InputContext.BillingSearch, elapsedMs: 2000);
        var resultFast = await _sut.ClassifyAsync(
            "12345678", InputContext.BillingSearch, elapsedMs: 50);

        Assert.True(resultFast.Confidence > resultSlow.Confidence);
    }

    [Fact]
    public async Task Classify_ShortNumeric_DoesNotMatchBarcode()
    {
        var result = await _sut.ClassifyAsync(
            "12345", InputContext.BillingSearch);

        Assert.NotEqual(InputIntent.BarcodeScan, result.Intent);
    }

    [Fact]
    public async Task Classify_15DigitNumeric_DoesNotMatchBarcode()
    {
        var result = await _sut.ClassifyAsync(
            "123456789012345", InputContext.BillingSearch);

        // 15 digits is too long for standard barcode formats
        Assert.NotEqual(InputIntent.BarcodeScan, result.Intent);
    }

    [Fact]
    public async Task Classify_Barcode_InPinContext_ReturnsUnknown()
    {
        // PIN context takes priority — 8 digits in PIN context
        // doesn't match 4 or 6 so it's unknown
        var result = await _sut.ClassifyAsync(
            "12345678", InputContext.PinEntry);

        Assert.Equal(InputIntent.Unknown, result.Intent);
    }

    // ── Auto-complete detection ──────────────────────────────────────

    [Fact]
    public async Task Classify_TextInProductSearch_ReturnsAutoComplete()
    {
        var result = await _sut.ClassifyAsync(
            "shi", InputContext.ProductSearch);

        Assert.Equal(InputIntent.AutoCompleteTrigger, result.Intent);
        Assert.True(result.Confidence >= 0.7);
    }

    [Fact]
    public async Task Classify_TextInBillingSearch_ReturnsAutoComplete()
    {
        var result = await _sut.ClassifyAsync(
            "cot", InputContext.BillingSearch);

        Assert.Equal(InputIntent.AutoCompleteTrigger, result.Intent);
        Assert.True(result.Confidence >= 0.7);
    }

    [Fact]
    public async Task Classify_SingleChar_DoesNotTriggerAutoComplete()
    {
        var result = await _sut.ClassifyAsync(
            "s", InputContext.ProductSearch);

        Assert.NotEqual(InputIntent.AutoCompleteTrigger, result.Intent);
    }

    [Fact]
    public async Task Classify_NumericInSearch_DoesNotTriggerAutoComplete()
    {
        // Pure numeric input in search → barcode, not auto-complete
        var result = await _sut.ClassifyAsync(
            "123", InputContext.ProductSearch);

        Assert.NotEqual(InputIntent.AutoCompleteTrigger, result.Intent);
    }

    [Fact]
    public async Task Classify_3CharsInGeneral_ReturnsAutoComplete()
    {
        var result = await _sut.ClassifyAsync(
            "abc", InputContext.General);

        Assert.Equal(InputIntent.AutoCompleteTrigger, result.Intent);
        Assert.True(result.Confidence >= 0.7);
    }

    [Fact]
    public async Task Classify_2CharsInGeneral_BelowThreshold()
    {
        var result = await _sut.ClassifyAsync(
            "ab", InputContext.General);

        // 2 chars in General context → confidence 0.6, below 0.7 threshold
        Assert.True(result.Confidence < 0.7 || result.Intent == InputIntent.Unknown);
    }

    // ── Event publishing ─────────────────────────────────────────────

    [Fact]
    public async Task Classify_AboveThreshold_PublishesEvent()
    {
        await _sut.ClassifyAsync("4829", InputContext.PinEntry);

        await _eventBus.Received(1).PublishAsync(
            Arg.Is<IntentDetectedEvent>(e =>
                e.Result.Intent == InputIntent.PinCompleted));
    }

    [Fact]
    public async Task Classify_BelowThreshold_DoesNotPublish()
    {
        await _sut.ClassifyAsync("a", InputContext.General);

        await _eventBus.DidNotReceive()
            .PublishAsync(Arg.Any<IntentDetectedEvent>());
    }

    // ── Edge cases ───────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Classify_EmptyInput_ReturnsUnknown(string? input)
    {
        var result = await _sut.ClassifyAsync(input!, InputContext.General);

        Assert.Equal(InputIntent.Unknown, result.Intent);
        Assert.Equal(0.0, result.Confidence);
    }

    [Fact]
    public async Task Classify_WhitespaceAroundBarcode_IsTrimmed()
    {
        var result = await _sut.ClassifyAsync(
            "  4006381333931  ", InputContext.BillingSearch);

        Assert.Equal(InputIntent.BarcodeScan, result.Intent);
        Assert.Equal("4006381333931", result.ResolvedValue);
    }

    // ── IntentResult record ──────────────────────────────────────────

    [Fact]
    public void IntentResult_None_ReturnsUnknownWithZeroConfidence()
    {
        var result = IntentResult.None("test", InputContext.General);

        Assert.Equal(InputIntent.Unknown, result.Intent);
        Assert.Equal(0.0, result.Confidence);
        Assert.Equal("test", result.RawInput);
        Assert.Equal(InputContext.General, result.Context);
        Assert.Null(result.ResolvedValue);
    }

    [Fact]
    public void IntentResult_RecordEquality()
    {
        var a = new IntentResult
        {
            Intent = InputIntent.PinCompleted,
            Confidence = 1.0,
            RawInput = "1234",
            Context = InputContext.PinEntry,
            ResolvedValue = "UserPin"
        };
        var b = new IntentResult
        {
            Intent = InputIntent.PinCompleted,
            Confidence = 1.0,
            RawInput = "1234",
            Context = InputContext.PinEntry,
            ResolvedValue = "UserPin"
        };

        Assert.Equal(a, b);
    }

    // ── EAN-13 check digit validation ────────────────────────────────

    [Theory]
    [InlineData("4006381333931", true)]   // valid
    [InlineData("5901234123457", true)]   // valid
    [InlineData("4006381333932", false)]  // wrong check digit
    [InlineData("1234567890123", false)]  // wrong check digit
    public async Task Classify_Ean13CheckDigit_AffectsConfidence(
        string barcode, bool isValid)
    {
        var result = await _sut.ClassifyAsync(
            barcode, InputContext.BillingSearch);

        Assert.Equal(InputIntent.BarcodeScan, result.Intent);
        if (isValid)
            Assert.True(result.Confidence >= 0.9);
        else
            Assert.True(result.Confidence < 0.9);
    }

    // ── Confidence threshold property ────────────────────────────────

    [Fact]
    public void ConfidenceThreshold_IsPointSeven()
    {
        Assert.Equal(0.7, _sut.ConfidenceThreshold);
    }
}
