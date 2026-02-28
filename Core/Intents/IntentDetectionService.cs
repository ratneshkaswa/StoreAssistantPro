using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;

namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Singleton implementation of <see cref="IIntentDetectionService"/>.
/// <para>
/// Classification pipeline (first match wins at High confidence):
/// <list type="number">
///   <item><b>PIN completed</b> — context is <see cref="InputContext.PinEntry"/>
///         and input is 4 or 6 all-digits.</item>
///   <item><b>Barcode scan</b> — input matches barcode format
///         (8–13 digits, or Code128 alphanumeric) and was entered
///         rapidly (&lt; 150 ms) suggesting a hardware scanner.</item>
///   <item><b>Exact product match</b> — input matches a known barcode,
///         SKU, or exact product name in the catalog.</item>
///   <item><b>Auto-complete trigger</b> — input is ≥ 2 non-numeric
///         characters in a search context.</item>
///   <item><b>Unknown</b> — no pattern matched.</item>
/// </list>
/// </para>
/// </summary>
public sealed partial class IntentDetectionService : IIntentDetectionService
{
    private readonly IEventBus _eventBus;
    private readonly IPerformanceMonitor _perf;
    private readonly ILogger<IntentDetectionService> _logger;

    /// <summary>Maximum milliseconds for a burst to be considered scanner input.</summary>
    private const double ScannerTimingThresholdMs = 150.0;

    /// <summary>Minimum characters for auto-complete to kick in.</summary>
    private const int AutoCompleteMinLength = 2;

    public IntentDetectionService(
        IEventBus eventBus,
        IPerformanceMonitor perf,
        ILogger<IntentDetectionService> logger)
    {
        _eventBus = eventBus;
        _perf = perf;
        _logger = logger;
    }

    /// <inheritdoc/>
    public double ConfidenceThreshold => 0.7;

    /// <inheritdoc/>
    public async Task<IntentResult> ClassifyAsync(
        string input, InputContext context, double? elapsedMs = null)
    {
        if (string.IsNullOrWhiteSpace(input))
            return IntentResult.None(input ?? string.Empty, context);

        using var scope = _perf.BeginScope(
            "IntentDetection.Classify", TimeSpan.FromMilliseconds(10));

        var trimmed = input.Trim();
        var result = Classify(trimmed, context, elapsedMs);

        if (result.Confidence >= ConfidenceThreshold)
        {
            _logger.LogDebug(
                "Intent detected: {Intent} (confidence {Confidence:P0}) for input '{Input}' in {Context}",
                result.Intent, result.Confidence, trimmed, context);

            await _eventBus
                .PublishAsync(new IntentDetectedEvent(result))
                .ConfigureAwait(false);
        }

        return result;
    }

    // ── Classification pipeline ──────────────────────────────────────

    private static IntentResult Classify(
        string input, InputContext context, double? elapsedMs)
    {
        // 1. PIN completed — highest priority in PIN context
        var pin = TryClassifyPin(input, context);
        if (pin is not null) return pin;

        // 2. Barcode scan — rapid entry of barcode-formatted string
        var barcode = TryClassifyBarcode(input, context, elapsedMs);
        if (barcode is not null) return barcode;

        // 3. Auto-complete trigger — non-numeric search input ≥ threshold
        var autoComplete = TryClassifyAutoComplete(input, context);
        if (autoComplete is not null) return autoComplete;

        // 4. Unknown
        return IntentResult.None(input, context);
    }

    // ── PIN detection ────────────────────────────────────────────────

    private static IntentResult? TryClassifyPin(string input, InputContext context)
    {
        // Only fires in PIN entry context
        if (context != InputContext.PinEntry)
            return null;

        if (!IsAllDigits(input))
            return null;

        // 4-digit user PIN or 6-digit master PIN
        if (input.Length is 4 or 6)
        {
            return new IntentResult
            {
                Intent = InputIntent.PinCompleted,
                Confidence = 1.0,
                RawInput = input,
                Context = context,
                ResolvedValue = input.Length == 4 ? "UserPin" : "MasterPin"
            };
        }

        return null;
    }

    // ── Barcode detection ────────────────────────────────────────────

    private static IntentResult? TryClassifyBarcode(
        string input, InputContext context, double? elapsedMs)
    {
        // Barcode detection works in Billing and Product search contexts
        if (context is InputContext.PinEntry)
            return null;

        var confidence = 0.0;

        // Format check: EAN-8, EAN-13, UPC-A, or general numeric barcode
        if (IsAllDigits(input) && input.Length is >= 8 and <= 14)
        {
            confidence = 0.6;

            // EAN-13 with valid check digit → very high confidence
            if (input.Length == 13 && IsValidEan13CheckDigit(input))
                confidence = 0.95;
            else if (input.Length is 8 or 12 or 14)
                confidence = 0.8;
        }
        // Code128 / alphanumeric barcode pattern (must have at least one letter)
        else if (!IsAllDigits(input) && Code128Pattern().IsMatch(input))
        {
            confidence = 0.5;
        }

        if (confidence < 0.5)
            return null;

        // Timing boost: scanner devices type entire barcode in < 150ms
        if (elapsedMs.HasValue && elapsedMs.Value < ScannerTimingThresholdMs)
            confidence = Math.Min(1.0, confidence + 0.25);

        // Context boost: billing search is the primary barcode entry point
        if (context == InputContext.BillingSearch)
            confidence = Math.Min(1.0, confidence + 0.1);

        if (confidence < 0.5)
            return null;

        return new IntentResult
        {
            Intent = InputIntent.BarcodeScan,
            Confidence = confidence,
            RawInput = input,
            Context = context,
            ResolvedValue = input
        };
    }

    // ── Auto-complete detection ──────────────────────────────────────

    private static IntentResult? TryClassifyAutoComplete(
        string input, InputContext context)
    {
        if (context is InputContext.PinEntry)
            return null;

        if (input.Length < AutoCompleteMinLength)
            return null;

        // Pure numeric input in search contexts is more likely a barcode
        // or quantity — don't trigger auto-complete
        if (IsAllDigits(input))
            return null;

        var confidence = context switch
        {
            InputContext.ProductSearch => 0.9,
            InputContext.BillingSearch => 0.85,
            InputContext.General when input.Length >= 3 => 0.75,
            InputContext.General => 0.6,
            _ => 0.0
        };

        if (confidence < 0.5)
            return null;

        return new IntentResult
        {
            Intent = InputIntent.AutoCompleteTrigger,
            Confidence = confidence,
            RawInput = input,
            Context = context
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────

    /// <summary>All characters are ASCII digits.</summary>
    private static bool IsAllDigits(string value) =>
        value.Length > 0 && value.AsSpan().IndexOfAnyExceptInRange('0', '9') < 0;

    /// <summary>
    /// Validates the EAN-13 check digit (last digit) using the
    /// standard weighted-sum algorithm.
    /// </summary>
    private static bool IsValidEan13CheckDigit(string ean13)
    {
        if (ean13.Length != 13 || !IsAllDigits(ean13))
            return false;

        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = ean13[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        var checkDigit = (10 - (sum % 10)) % 10;
        return checkDigit == (ean13[12] - '0');
    }

    /// <summary>
    /// Matches alphanumeric barcode patterns (Code128-style):
    /// 6–20 chars, letters/digits/hyphens, at least one digit.
    /// </summary>
    [GeneratedRegex(@"^[A-Za-z0-9\-]{6,20}$")]
    private static partial Regex Code128Pattern();
}
