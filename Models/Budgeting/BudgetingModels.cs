namespace StoreAssistantPro.Models.Budgeting;

/// <summary>Monthly budget entry (#934-935).</summary>
public sealed class BudgetEntry
{
    public int Id { get; set; }
    public string BudgetType { get; set; } = "Sales"; // Sales, Expense
    public string? Category { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
}

/// <summary>Budget vs actual comparison (#936-937).</summary>
public sealed record BudgetVariance(
    string BudgetType,
    string? Category,
    int Month,
    int Year,
    decimal BudgetAmount,
    decimal ActualAmount,
    decimal VarianceAmount,
    double VariancePercent);

/// <summary>Financial goal (#939).</summary>
public sealed class FinancialGoal
{
    public int Id { get; set; }
    public string GoalName { get; set; } = string.Empty;
    public string MetricType { get; set; } = "Revenue"; // Revenue, Profit, Margin
    public decimal TargetValue { get; set; }
    public decimal CurrentValue { get; set; }
    public DateTime TargetDate { get; set; }
    public bool IsAchieved { get; set; }
}
