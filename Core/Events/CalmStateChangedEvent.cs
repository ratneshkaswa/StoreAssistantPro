using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published by <see cref="Services.ICalmUIService"/> whenever the
/// emphasis map changes. XAML behaviors (<c>BillingDimBehavior</c>,
/// <c>AdaptiveWorkspace</c>) and ViewModels can subscribe to adapt
/// their visual state without polling.
/// </summary>
/// <param name="ActiveZone">The zone with primary user attention.</param>
/// <param name="CalmModeEnabled"><c>true</c> when calm system is active.</param>
/// <param name="FlowState">
/// The operator's current cognitive engagement level. When
/// <see cref="Models.FlowState.Flow"/>, XAML behaviors should apply
/// maximum noise reduction beyond normal calm emphasis.
/// </param>
public sealed record CalmStateChangedEvent(
    WorkspaceZone ActiveZone,
    bool CalmModeEnabled,
    FlowState FlowState = FlowState.Calm) : IEvent;
