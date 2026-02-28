using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Published when a zero-click PIN auto-submission fails.
/// <para>
/// Subscribers should:
/// <list type="bullet">
///   <item>Clear the PIN pad.</item>
///   <item>Display the <see cref="ErrorMessage"/> inline.</item>
///   <item>Return focus to the PIN input.</item>
/// </list>
/// </para>
/// </summary>
/// <param name="PinType"><c>"UserPin"</c> or <c>"MasterPin"</c>.</param>
/// <param name="ErrorMessage">Human-readable failure reason.</param>
public sealed record PinSubmissionFailedEvent(
    string PinType,
    string ErrorMessage) : IEvent;
