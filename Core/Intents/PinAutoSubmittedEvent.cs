using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Published when a zero-click PIN auto-submission succeeds.
/// Consumers can close the login window, proceed to the next step, etc.
/// </summary>
/// <param name="PinType"><c>"UserPin"</c> or <c>"MasterPin"</c>.</param>
public sealed record PinAutoSubmittedEvent(string PinType) : IEvent;
