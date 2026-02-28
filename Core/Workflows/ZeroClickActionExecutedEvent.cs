using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Published when a <see cref="IZeroClickWorkflowService"/> rule fires
/// and automatically executes an action on the user's behalf.
/// <para>
/// Subscribers (e.g., toast service, audit log) can use this to inform
/// the user that an automatic action was taken without their explicit click.
/// </para>
/// </summary>
public sealed record ZeroClickActionExecutedEvent(
    string RuleId,
    string Description,
    DateTime ExecutedAt) : IEvent;
