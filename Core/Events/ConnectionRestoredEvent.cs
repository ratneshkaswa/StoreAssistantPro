namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published when the <see cref="Services.IConnectivityMonitorService"/>
/// detects that database connectivity has been restored after a
/// <see cref="ConnectionLostEvent"/>.
/// </summary>
/// <param name="DowntimeDuration">
/// How long the connection was unavailable.
/// </param>
public sealed record ConnectionRestoredEvent(
    TimeSpan DowntimeDuration) : IEvent;
