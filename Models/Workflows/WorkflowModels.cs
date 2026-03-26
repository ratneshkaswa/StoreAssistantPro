namespace StoreAssistantPro.Models.Workflows;

/// <summary>
/// Automation rule definition for workflow triggers (#890-897).
/// </summary>
public sealed class AutomationRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty; // LowStock, DayEnd, SaleComplete, CreditLimit, Reorder, Expiry, Milestone, Schedule
    public string ActionType { get; set; } = string.Empty; // CreatePO, Backup, Print, Alert, Reorder, Markdown, UpgradeTier, GenerateReport
    public bool IsEnabled { get; set; } = true;
    public string? ConditionJson { get; set; }
    public string? ActionConfigJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastTriggeredAt { get; set; }
    public int TriggerCount { get; set; }
}

/// <summary>
/// Automation trigger event that fired a rule.
/// </summary>
public sealed record AutomationTriggerEvent(
    string TriggerType,
    int? RelatedEntityId,
    string? RelatedEntityType,
    Dictionary<string, object?> Context);

/// <summary>
/// Result of executing an automation rule.
/// </summary>
public sealed record AutomationResult(
    bool Success,
    string RuleName,
    string ActionType,
    string? Message,
    DateTime ExecutedAt);
