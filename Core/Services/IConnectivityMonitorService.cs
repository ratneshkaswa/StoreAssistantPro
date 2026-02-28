namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Monitors database connectivity on a background timer and publishes
/// <see cref="Events.ConnectionLostEvent"/> /
/// <see cref="Events.ConnectionRestoredEvent"/> via the
/// <see cref="Events.IEventBus"/>.
/// <para>
/// <b>Lifecycle:</b> Registered as a <b>singleton</b>. Call
/// <see cref="StartAsync"/> once after the host is built (typically in
/// <c>App.OnStartup</c>). The monitor runs until
/// <see cref="IDisposable.Dispose"/> is called at shutdown.
/// </para>
/// <para>
/// <b>Thread safety:</b> All state transitions are protected by a lock.
/// The <see cref="IsConnected"/> property is safe to read from any thread.
/// </para>
/// </summary>
public interface IConnectivityMonitorService : IDisposable
{
    /// <summary>
    /// Current connectivity state. <c>true</c> when the last health
    /// check succeeded; <c>false</c> after a
    /// <see cref="Events.ConnectionLostEvent"/>.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Performs an initial connectivity check and starts the periodic
    /// background timer. Safe to call only once.
    /// </summary>
    Task StartAsync(CancellationToken ct = default);
}
