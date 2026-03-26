using StoreAssistantPro.Models.Budgeting;

namespace StoreAssistantPro.Modules.Budgeting.Services;

/// <summary>Budgeting service (#934-937).</summary>
public interface IBudgetService
{
    Task<BudgetEntry> SaveBudgetAsync(BudgetEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<BudgetEntry>> GetBudgetsAsync(int year, string? budgetType = null, CancellationToken ct = default);
    Task<IReadOnlyList<BudgetVariance>> GetVarianceReportAsync(int year, CancellationToken ct = default);
    Task RefreshActualsAsync(int month, int year, CancellationToken ct = default);
}

/// <summary>Forecasting service (#938).</summary>
public interface IBudgetForecastService
{
    Task<IReadOnlyList<BudgetEntry>> ForecastAsync(int months, CancellationToken ct = default);
}

/// <summary>Financial goal tracking service (#939).</summary>
public interface IFinancialGoalService
{
    Task<IReadOnlyList<FinancialGoal>> GetGoalsAsync(CancellationToken ct = default);
    Task<FinancialGoal> SaveGoalAsync(FinancialGoal goal, CancellationToken ct = default);
    Task RefreshProgressAsync(CancellationToken ct = default);
}
