namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Pure logic service consulted by <see cref="Helpers.KeyboardNav"/>
/// on every Enter keypress to decide between auto-executing a
/// high-confidence action and standard field navigation.
///
/// <para><b>Decision matrix:</b></para>
/// <code>
///   ┌────────────────────────┬───────────────────────────────────────┐
///   │ Situation              │ Decision                              │
///   ├────────────────────────┼───────────────────────────────────────┤
///   │ Billing search + text  │ Evaluate latest intent → Execute if  │
///   │   matches one product  │   high confidence, else MoveNext     │
///   │ PIN field at length    │ Auto-submit PIN (handled separately) │
///   │ Any form field         │ MoveNext (standard navigation)       │
///   │ Previous action busy   │ Suppress                             │
///   └────────────────────────┴───────────────────────────────────────┘
/// </code>
///
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>No WPF types — pure logic, fully unit-testable.</item>
///   <item>Subscribes to <see cref="IntentDetectedEvent"/> to cache the
///         latest classification result as the user types.</item>
///   <item><see cref="Evaluate"/> is <b>synchronous</b> so it can be
///         called from WPF's <c>PreviewKeyDown</c> handler.</item>
///   <item>The actual execution is done by zero-click services that
///         subscribe to the <see cref="IntentDetectedEvent"/> pipeline.
///         This service only decides whether Enter should trigger them
///         or do standard navigation.</item>
/// </list>
/// </summary>
public interface ISmartEnterKeyService : IDisposable
{
    /// <summary>
    /// Synchronous evaluation for the Enter keypress handler.
    /// Consults the latest cached intent classification to decide.
    /// </summary>
    /// <param name="inputText">Current text in the focused input field.</param>
    /// <param name="context">Active input context.</param>
    EnterKeyDecision Evaluate(string inputText, InputContext context);

    /// <summary>
    /// Updates the cached intent result for the given input text.
    /// Called by the intent detection pipeline as the user types.
    /// </summary>
    void UpdateLatestIntent(IntentResult intent);

    /// <summary>
    /// Clears the cached intent (e.g., when the input field is cleared
    /// or focus moves away).
    /// </summary>
    void ClearLatestIntent();

    /// <summary>
    /// The most recently cached intent, or <c>null</c> if none.
    /// </summary>
    IntentResult? LatestIntent { get; }
}
