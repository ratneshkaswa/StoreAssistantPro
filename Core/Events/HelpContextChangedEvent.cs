using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published by <see cref="Services.IContextHelpService"/> when the
/// application context changes in a way that affects help content
/// (e.g. mode switch, connectivity change, focus lock transition).
/// <para>
/// Subscribers can use this to refresh context-sensitive tooltips,
/// help panels, or guided walkthroughs without polling AppState.
/// </para>
/// </summary>
public sealed record HelpContextChangedEvent(HelpContext Context) : IEvent;
