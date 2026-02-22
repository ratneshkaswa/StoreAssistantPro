namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published by <see cref="Services.IOfflineModeService"/> when the
/// application transitions between online and offline mode.
/// <para>
/// Subscribers can use this to disable/enable UI elements, queue
/// operations, or show notifications without coupling to
/// <see cref="Services.IAppStateService"/>.
/// </para>
/// </summary>
/// <param name="IsOffline">
/// <c>true</c> when the app has entered offline mode;
/// <c>false</c> when connectivity is restored.
/// </param>
/// <param name="DowntimeDuration">
/// When <paramref name="IsOffline"/> is <c>false</c>, carries the
/// duration the connection was unavailable. <see cref="TimeSpan.Zero"/>
/// when entering offline mode.
/// </param>
public sealed record OfflineModeChangedEvent(
    bool IsOffline,
    TimeSpan DowntimeDuration) : IEvent;
