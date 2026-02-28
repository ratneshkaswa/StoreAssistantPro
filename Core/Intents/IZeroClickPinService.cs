namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Reusable zero-click PIN submission service. Bridges the intent
/// detection system to any PIN consumer (login, approval, PIN change).
/// <para>
/// <b>Behavior:</b>
/// <list type="bullet">
///   <item>When required PIN length is reached, auto-submits via the
///         registered <see cref="PinSubmitHandler"/>.</item>
///   <item>No login button required — submission is automatic.</item>
///   <item>On failure: publishes <see cref="PinSubmissionFailedEvent"/>
///         so the PIN pad is cleared and an inline error is shown.</item>
///   <item>On success: publishes <see cref="PinAutoSubmittedEvent"/>.</item>
/// </list>
/// </para>
///
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>No UI dependency — pure service logic.</item>
///   <item>Consumers register a submit handler via <see cref="RegisterHandler"/>
///         and unregister via <see cref="UnregisterHandler"/>.</item>
///   <item>Works with <see cref="IPredictiveFocusService"/> for focus
///         return on failure.</item>
/// </list>
/// </summary>
public interface IZeroClickPinService : IDisposable
{
    /// <summary>
    /// Delegate that performs the actual PIN validation/submission.
    /// Returns a <see cref="PinSubmissionResult"/> indicating success or failure.
    /// </summary>
    /// <param name="pin">The complete PIN string.</param>
    /// <param name="pinType"><c>"UserPin"</c> or <c>"MasterPin"</c>.</param>
    delegate Task<PinSubmissionResult> PinSubmitHandler(string pin, string pinType);

    /// <summary>
    /// Registers a handler to receive auto-submitted PINs.
    /// Only one handler can be active at a time (last registration wins).
    /// </summary>
    /// <param name="handler">Async callback that validates the PIN.</param>
    /// <param name="focusElementName">
    /// The <c>x:Name</c> of the PIN input element to refocus on failure.
    /// </param>
    void RegisterHandler(PinSubmitHandler handler, string focusElementName);

    /// <summary>
    /// Removes the current handler registration.
    /// </summary>
    void UnregisterHandler();

    /// <summary>
    /// <c>true</c> when a handler is registered and the service is
    /// ready to process PIN completions.
    /// </summary>
    bool IsHandlerRegistered { get; }

    /// <summary>
    /// <c>true</c> while an async PIN submission is in progress.
    /// Prevents double-submission if the user manages to type another
    /// digit during the async operation.
    /// </summary>
    bool IsSubmitting { get; }
}
