namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Mediates between <see cref="IConnectivityMonitorService"/> events
/// and the rest of the application. When connectivity is lost the
/// service switches the app into offline mode; when restored it
/// switches back.
/// <para>
/// <b>Responsibilities:</b>
/// </para>
/// <list type="bullet">
///   <item>Subscribes to <see cref="Events.ConnectionLostEvent"/> and
///         <see cref="Events.ConnectionRestoredEvent"/>.</item>
///   <item>Updates <see cref="IAppStateService.IsOfflineMode"/> and
///         <see cref="IAppStateService.LastConnectionCheck"/>.</item>
///   <item>Publishes <see cref="Events.OfflineModeChangedEvent"/> so
///         other services can react without polling AppState.</item>
///   <item>Posts status bar messages for operator awareness.</item>
/// </list>
/// <para>
/// Registered as a <b>singleton</b>. Implements <see cref="IDisposable"/>
/// to unsubscribe from the event bus at shutdown.
/// </para>
/// </summary>
public interface IOfflineModeService : IDisposable
{
    /// <summary>
    /// Current offline state. <c>true</c> when the database is
    /// unreachable; mirrors <see cref="IAppStateService.IsOfflineMode"/>.
    /// </summary>
    bool IsOffline { get; }
}
