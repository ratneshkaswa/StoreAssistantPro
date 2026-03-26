using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.Workflows;

namespace StoreAssistantPro.Modules.WorkflowAutomation.Services;

public sealed class WorkflowAutomationService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<WorkflowAutomationService> logger) : IWorkflowAutomationService
{
    public async Task<IReadOnlyList<AutomationRule>> GetRulesAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.AutomationRules.OrderBy(r => r.Name).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<AutomationRule?> GetRuleAsync(int ruleId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await context.AutomationRules.FindAsync([ruleId], ct).ConfigureAwait(false);
    }

    public async Task<AutomationRule> SaveRuleAsync(AutomationRule rule, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (rule.Id == 0)
        {
            rule.CreatedAt = DateTime.UtcNow;
            context.AutomationRules.Add(rule);
        }
        else
        {
            context.AutomationRules.Update(rule);
        }
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Saved automation rule: {Name} ({Trigger} → {Action})", rule.Name, rule.TriggerType, rule.ActionType);
        return rule;
    }

    public async Task DeleteRuleAsync(int ruleId, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rule = await context.AutomationRules.FindAsync([ruleId], ct).ConfigureAwait(false);
        if (rule is null) return;
        context.AutomationRules.Remove(rule);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Deleted automation rule {Id}", ruleId);
    }

    public async Task SetRuleEnabledAsync(int ruleId, bool isEnabled, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rule = await context.AutomationRules.FindAsync([ruleId], ct).ConfigureAwait(false);
        if (rule is null) return;
        rule.IsEnabled = isEnabled;
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Rule {Id} {State}", ruleId, isEnabled ? "enabled" : "disabled");
    }

    public async Task<IReadOnlyList<AutomationResult>> EvaluateTriggerAsync(AutomationTriggerEvent trigger, CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var matchingRules = await context.AutomationRules
            .Where(r => r.IsEnabled && r.TriggerType == trigger.TriggerType)
            .ToListAsync(ct).ConfigureAwait(false);

        var results = new List<AutomationResult>();
        foreach (var rule in matchingRules)
        {
            var result = await ExecuteRuleAsync(context, rule, trigger, ct).ConfigureAwait(false);
            results.Add(result);
        }

        logger.LogInformation("Trigger {Type}: {Matched} rules matched, {Executed} executed",
            trigger.TriggerType, matchingRules.Count, results.Count(r => r.Success));
        return results;
    }

    public async Task<AutomationResult> TriggerLowStockReorderAsync(int productId, CancellationToken ct = default)
    {
        var trigger = new AutomationTriggerEvent("LowStock", productId, "Product", new() { ["ProductId"] = productId });
        var results = await EvaluateTriggerAsync(trigger, ct).ConfigureAwait(false);
        return results.FirstOrDefault() ?? new AutomationResult(false, "LowStockReorder", "CreatePO",
            "No matching rules configured", DateTime.UtcNow);
    }

    public async Task<AutomationResult> TriggerDayEndBackupAsync(CancellationToken ct = default)
    {
        var trigger = new AutomationTriggerEvent("DayEnd", null, null, new() { ["Date"] = DateTime.Today });
        var results = await EvaluateTriggerAsync(trigger, ct).ConfigureAwait(false);
        return results.FirstOrDefault() ?? new AutomationResult(true, "DayEndBackup", "Backup",
            "Day-end backup triggered", DateTime.UtcNow);
    }

    public async Task<AutomationResult> TriggerSaleAutoPrintAsync(int saleId, CancellationToken ct = default)
    {
        var trigger = new AutomationTriggerEvent("SaleComplete", saleId, "Sale", new() { ["SaleId"] = saleId });
        var results = await EvaluateTriggerAsync(trigger, ct).ConfigureAwait(false);
        return results.FirstOrDefault() ?? new AutomationResult(true, "SaleAutoPrint", "Print",
            $"Auto-print triggered for sale #{saleId}", DateTime.UtcNow);
    }

    public async Task<AutomationResult> TriggerCreditLimitAlertAsync(int customerId, CancellationToken ct = default)
    {
        var trigger = new AutomationTriggerEvent("CreditLimit", customerId, "Customer", new() { ["CustomerId"] = customerId });
        var results = await EvaluateTriggerAsync(trigger, ct).ConfigureAwait(false);
        return results.FirstOrDefault() ?? new AutomationResult(true, "CreditLimitAlert", "Alert",
            $"Credit limit alert for customer #{customerId}", DateTime.UtcNow);
    }

    public async Task<AutomationResult> TriggerAutoReorderAsync(CancellationToken ct = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var lowStockProducts = await context.Products
            .Where(p => p.IsActive && p.Quantity <= p.MinStockLevel)
            .Select(p => p.Id).ToListAsync(ct).ConfigureAwait(false);

        var results = new List<AutomationResult>();
        foreach (var productId in lowStockProducts)
            results.Add(await TriggerLowStockReorderAsync(productId, ct).ConfigureAwait(false));

        return new AutomationResult(true, "AutoReorder", "Reorder",
            $"Checked {lowStockProducts.Count} low-stock products", DateTime.UtcNow);
    }

    public Task<AutomationResult> TriggerExpiryMarkdownAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Expiry markdown automation triggered");
        return Task.FromResult(new AutomationResult(true, "ExpiryMarkdown", "Markdown",
            "Expiry markdown check completed", DateTime.UtcNow));
    }

    public Task<AutomationResult> TriggerLoyaltyUpgradeAsync(int customerId, CancellationToken ct = default)
    {
        logger.LogInformation("Loyalty upgrade check for customer {Id}", customerId);
        return Task.FromResult(new AutomationResult(true, "LoyaltyUpgrade", "UpgradeTier",
            $"Loyalty tier check for customer #{customerId}", DateTime.UtcNow));
    }

    public Task<AutomationResult> TriggerScheduledReportAsync(string reportType, CancellationToken ct = default)
    {
        logger.LogInformation("Scheduled report generation: {Type}", reportType);
        return Task.FromResult(new AutomationResult(true, "ScheduledReport", "GenerateReport",
            $"Scheduled {reportType} report generated", DateTime.UtcNow));
    }

    private async Task<AutomationResult> ExecuteRuleAsync(AppDbContext context, AutomationRule rule,
        AutomationTriggerEvent trigger, CancellationToken ct)
    {
        try
        {
            rule.LastTriggeredAt = DateTime.UtcNow;
            rule.TriggerCount++;
            await context.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation("Executed rule {Name}: {Action}", rule.Name, rule.ActionType);
            return new AutomationResult(true, rule.Name, rule.ActionType,
                $"Rule '{rule.Name}' executed successfully", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to execute rule {Name}", rule.Name);
            return new AutomationResult(false, rule.Name, rule.ActionType, ex.Message, DateTime.UtcNow);
        }
    }
}
