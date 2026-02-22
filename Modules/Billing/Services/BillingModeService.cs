using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Session-aware mode-switching service. Coordinates
/// <see cref="IAppStateService"/>, <see cref="IFeatureToggleService"/>,
/// and <see cref="IEventBus"/> to ensure all subscribers are notified
/// of mode transitions.
/// <para>
/// <b>Safety rule:</b> <see cref="StopBillingAsync"/> is deferred while
/// a billing session is <see cref="BillingSessionState.Active"/>. The
/// deferred request is flushed automatically by
/// <see cref="SmartBillingModeService"/> when the session ends, or can
/// be flushed explicitly via <see cref="FlushDeferredStopAsync"/>.
/// </para>
/// </summary>
public class BillingModeService(
    IAppStateService appState,
    IFeatureToggleService featureToggle,
    IEventBus eventBus) : IBillingModeService
{
    private readonly object _lock = new();

    public OperationalMode CurrentMode => appState.CurrentMode;

    public bool IsStopDeferred { get; private set; }

    public Task StartBillingAsync()
    {
        // Starting billing always clears a pending deferred stop — the
        // operator explicitly chose to enter billing mode.
        lock (_lock)
        {
            IsStopDeferred = false;
        }

        return TransitionAsync(OperationalMode.Billing);
    }

    public Task StopBillingAsync()
    {
        // If a session is actively in progress, defer the stop so the
        // operator's cart is not disrupted.
        if (appState.CurrentBillingSession == BillingSessionState.Active)
        {
            lock (_lock)
            {
                IsStopDeferred = true;
            }
            return Task.CompletedTask;
        }

        lock (_lock)
        {
            IsStopDeferred = false;
        }

        return TransitionAsync(OperationalMode.Management);
    }

    public Task FlushDeferredStopAsync()
    {
        bool shouldStop;

        lock (_lock)
        {
            shouldStop = IsStopDeferred;
            IsStopDeferred = false;
        }

        return shouldStop
            ? TransitionAsync(OperationalMode.Management)
            : Task.CompletedTask;
    }

    private async Task TransitionAsync(OperationalMode target)
    {
        var previous = appState.CurrentMode;
        if (previous == target)
            return;

        appState.SetMode(target);
        featureToggle.SetMode(target);

        await eventBus.PublishAsync(
            new OperationalModeChangedEvent(previous, target));
    }
}
