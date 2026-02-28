namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// Singleton service that listens to zero-click action events and
/// publishes <see cref="MicroFeedbackEvent"/> for UI-side animation.
/// <para>
/// <b>Separation of concerns:</b> This service bridges the domain
/// event layer to the UI feedback layer. Zero-click services publish
/// domain events; this service translates them into feedback events;
/// attached behaviors consume the feedback events to drive animation.
/// </para>
///
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>No WPF dependency — pure service logic.</item>
///   <item>Subscribes to <c>ProductAddedToCartEvent</c>,
///         <c>PinAutoSubmittedEvent</c>, <c>ZeroClickActionExecutedEvent</c>.</item>
///   <item>Publishes <see cref="MicroFeedbackEvent"/> on the same
///         <see cref="Events.IEventBus"/>.</item>
///   <item>Calm UI compatible — the attached behavior checks calm state
///         before animating.</item>
/// </list>
/// </summary>
public interface IMicroFeedbackService : IDisposable;
