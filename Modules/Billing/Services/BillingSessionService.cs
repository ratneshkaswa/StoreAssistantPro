using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;

namespace StoreAssistantPro.Modules.Billing.Services;

/// <summary>
/// Singleton service that owns the billing session lifecycle.
/// Validates state transitions, updates <see cref="IAppStateService"/>,
/// and publishes both a granular event (<see cref="BillingSessionStartedEvent"/>,
/// <see cref="BillingSessionCompletedEvent"/>, <see cref="BillingSessionCancelledEvent"/>)
/// and the generic <see cref="BillingSessionStateChangedEvent"/> envelope.
/// </summary>
public class BillingSessionService(
    IAppStateService appState,
    IEventBus eventBus) : IBillingSessionService
{
    private readonly object _lock = new();

    public BillingSessionState CurrentState { get; private set; } = BillingSessionState.None;

    public Task StartSessionAsync() =>
        TransitionAsync(BillingSessionState.Active);

    public Task CompleteSessionAsync() =>
        TransitionAsync(BillingSessionState.Completed);

    public Task CancelSessionAsync() =>
        TransitionAsync(BillingSessionState.Cancelled);

    private async Task TransitionAsync(BillingSessionState target)
    {
        BillingSessionState previous;

        lock (_lock)
        {
            previous = CurrentState;
            ValidateTransition(previous, target);
            CurrentState = target;
        }

        appState.SetBillingSession(target);

        // Publish the granular event first so subscribers that only
        // care about one transition can react before the generic
        // envelope fires.
        await PublishGranularEventAsync(target);

        await eventBus.PublishAsync(
            new BillingSessionStateChangedEvent(previous, target));
    }

    private Task PublishGranularEventAsync(BillingSessionState state) => state switch
    {
        BillingSessionState.Active    => eventBus.PublishAsync(new BillingSessionStartedEvent()),
        BillingSessionState.Completed => eventBus.PublishAsync(new BillingSessionCompletedEvent()),
        BillingSessionState.Cancelled => eventBus.PublishAsync(new BillingSessionCancelledEvent()),
        _                             => Task.CompletedTask
    };

    private static void ValidateTransition(BillingSessionState from, BillingSessionState to)
    {
        var valid = (from, to) switch
        {
            // Starting a new session
            (BillingSessionState.None, BillingSessionState.Active)      => true,
            (BillingSessionState.Completed, BillingSessionState.Active) => true,
            (BillingSessionState.Cancelled, BillingSessionState.Active) => true,

            // Ending an active session
            (BillingSessionState.Active, BillingSessionState.Completed) => true,
            (BillingSessionState.Active, BillingSessionState.Cancelled) => true,

            // Returning to idle (stop billing)
            (BillingSessionState.Completed, BillingSessionState.None)   => true,
            (BillingSessionState.Cancelled, BillingSessionState.None)   => true,
            (BillingSessionState.None, BillingSessionState.None)        => true,

            _ => false
        };

        if (!valid)
            throw new InvalidOperationException(
                $"Cannot transition billing session from {from} to {to}.");
    }
}
