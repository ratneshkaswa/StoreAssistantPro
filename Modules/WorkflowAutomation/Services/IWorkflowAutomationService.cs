using StoreAssistantPro.Models.Workflows;

namespace StoreAssistantPro.Modules.WorkflowAutomation.Services;

/// <summary>
/// Workflow automation rule engine (#890-897).
/// Evaluates triggers and executes automated actions.
/// </summary>
public interface IWorkflowAutomationService
{
    /// <summary>Get all automation rules.</summary>
    Task<IReadOnlyList<AutomationRule>> GetRulesAsync(CancellationToken ct = default);

    /// <summary>Get a specific rule by ID.</summary>
    Task<AutomationRule?> GetRuleAsync(int ruleId, CancellationToken ct = default);

    /// <summary>Create or update an automation rule.</summary>
    Task<AutomationRule> SaveRuleAsync(AutomationRule rule, CancellationToken ct = default);

    /// <summary>Delete an automation rule.</summary>
    Task DeleteRuleAsync(int ruleId, CancellationToken ct = default);

    /// <summary>Enable or disable a rule.</summary>
    Task SetRuleEnabledAsync(int ruleId, bool isEnabled, CancellationToken ct = default);

    /// <summary>Evaluate a trigger event and execute matching rules (#890-897).</summary>
    Task<IReadOnlyList<AutomationResult>> EvaluateTriggerAsync(AutomationTriggerEvent trigger, CancellationToken ct = default);

    /// <summary>Low stock → auto PO (#890).</summary>
    Task<AutomationResult> TriggerLowStockReorderAsync(int productId, CancellationToken ct = default);

    /// <summary>Day end → auto backup (#891).</summary>
    Task<AutomationResult> TriggerDayEndBackupAsync(CancellationToken ct = default);

    /// <summary>Sale complete → auto print (#892).</summary>
    Task<AutomationResult> TriggerSaleAutoPrintAsync(int saleId, CancellationToken ct = default);

    /// <summary>Credit limit → auto alert (#893).</summary>
    Task<AutomationResult> TriggerCreditLimitAlertAsync(int customerId, CancellationToken ct = default);

    /// <summary>Inventory → auto reorder (#894).</summary>
    Task<AutomationResult> TriggerAutoReorderAsync(CancellationToken ct = default);

    /// <summary>Expiry → auto markdown (#895).</summary>
    Task<AutomationResult> TriggerExpiryMarkdownAsync(CancellationToken ct = default);

    /// <summary>Milestone → auto loyalty upgrade (#896).</summary>
    Task<AutomationResult> TriggerLoyaltyUpgradeAsync(int customerId, CancellationToken ct = default);

    /// <summary>Schedule → auto report (#897).</summary>
    Task<AutomationResult> TriggerScheduledReportAsync(string reportType, CancellationToken ct = default);
}
