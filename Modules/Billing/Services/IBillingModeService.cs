using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Controls the application's operational mode transitions.
/// <para>
/// <b>StartBilling</b> switches to POS mode — management features
/// are hidden, the UI is streamlined for counter operators.<br/>
/// <b>StopBilling</b> returns to management mode — full back-office
/// access is restored.
/// </para>
/// <para>
/// <b>Smart automation safety:</b> When a billing session is
/// <see cref="BillingSessionState.Active"/>, <see cref="StopBillingAsync"/>
/// is a <b>no-op</b> — the operator's cart is protected. Only after the
/// session completes or is cancelled will the mode switch succeed.
/// </para>
/// <para>
/// Each transition updates <see cref="Core.Services.IAppStateService.CurrentMode"/>
/// and publishes <see cref="Core.Events.OperationalModeChangedEvent"/>
/// via <see cref="Core.Events.IEventBus"/> so subscribers can react
/// without direct coupling.
/// </para>
/// </summary>
public interface IBillingModeService
{
    /// <summary>Current operational mode.</summary>
    OperationalMode CurrentMode { get; }

    /// <summary>
    /// <c>true</c> when <see cref="StopBillingAsync"/> was requested
    /// but deferred because a billing session is active. The switch
    /// will execute automatically when the session ends.
    /// </summary>
    bool IsStopDeferred { get; }

    /// <summary>
    /// Switches to <see cref="OperationalMode.Billing"/>.
    /// Always allowed. No-op if already in billing mode.
    /// </summary>
    Task StartBillingAsync();

    /// <summary>
    /// Returns to <see cref="OperationalMode.Management"/>.
    /// <para>
    /// If a billing session is <see cref="BillingSessionState.Active"/>,
    /// the request is <b>deferred</b> (see <see cref="IsStopDeferred"/>)
    /// until the session completes or is cancelled. Otherwise executes
    /// immediately. No-op if already in management mode.
    /// </para>
    /// </summary>
    Task StopBillingAsync();

    /// <summary>
    /// Executes a deferred stop if one is pending and the session is
    /// no longer active. Called by <see cref="SmartBillingModeService"/>
    /// after a session ends.
    /// </summary>
    Task FlushDeferredStopAsync();
}
