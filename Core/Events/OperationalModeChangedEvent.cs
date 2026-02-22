using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published when the application switches between
/// <see cref="OperationalMode.Management"/> and
/// <see cref="OperationalMode.Billing"/>.
/// <para>
/// Subscribers use this to reconfigure navigation, toolbar, keyboard
/// shortcuts, and feature visibility without tight coupling to
/// <see cref="Services.IAppStateService"/>.
/// </para>
/// </summary>
public sealed record OperationalModeChangedEvent(
    OperationalMode PreviousMode,
    OperationalMode NewMode) : IEvent;
