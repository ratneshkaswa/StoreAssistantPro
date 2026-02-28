namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Classifies raw user input into structured intents without any UI
/// dependency. Consumers subscribe to <see cref="IntentDetectedEvent"/>
/// to react to high-confidence classifications.
/// <para>
/// <b>Responsibilities:</b>
/// <list type="bullet">
///   <item>Detect barcode scans (format + timing heuristics).</item>
///   <item>Detect exact product matches (name, SKU, barcode catalog lookup).</item>
///   <item>Detect completed PIN entries (4 or 6 all-digit sequences).</item>
///   <item>Detect auto-complete trigger thresholds.</item>
/// </list>
/// </para>
/// <para>
/// <b>Architecture:</b> Registered as a <b>singleton</b>.
/// Pure classification logic — no UI references, no ViewModel coupling.
/// Publishes <see cref="IntentDetectedEvent"/> via
/// <see cref="Events.IEventBus"/> when confidence ≥
/// <see cref="ConfidenceThreshold"/>.
/// </para>
/// </summary>
public interface IIntentDetectionService
{
    /// <summary>
    /// Minimum confidence (0.0–1.0) required for an intent to be
    /// published as an <see cref="IntentDetectedEvent"/>.
    /// Default: <c>0.7</c>.
    /// </summary>
    double ConfidenceThreshold { get; }

    /// <summary>
    /// Classifies <paramref name="input"/> within the given
    /// <paramref name="context"/> and returns the best-matching intent.
    /// If confidence ≥ <see cref="ConfidenceThreshold"/>, an
    /// <see cref="IntentDetectedEvent"/> is also published.
    /// </summary>
    /// <param name="input">Raw text entered by the user.</param>
    /// <param name="context">Active input context.</param>
    /// <param name="elapsedMs">
    /// Milliseconds since the first character of this input burst.
    /// Used for barcode scanner timing heuristics (scanners type
    /// 8–13 digits in &lt; 100 ms). Pass <c>null</c> if unavailable.
    /// </param>
    Task<IntentResult> ClassifyAsync(
        string input,
        InputContext context,
        double? elapsedMs = null);
}
