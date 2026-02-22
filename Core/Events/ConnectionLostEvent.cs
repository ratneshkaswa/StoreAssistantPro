namespace StoreAssistantPro.Core.Events;

/// <summary>
/// Published when the <see cref="Services.IConnectivityMonitorService"/>
/// detects that the database is no longer reachable.
/// </summary>
public sealed record ConnectionLostEvent : IEvent;
