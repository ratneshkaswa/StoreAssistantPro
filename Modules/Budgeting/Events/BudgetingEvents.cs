using StoreAssistantPro.Core.Events;

namespace StoreAssistantPro.Modules.Budgeting.Events;

/// <summary>Published when a budget entry is created or updated.</summary>
public sealed class BudgetUpdatedEvent(int month, int year, string budgetType) : IEvent
{
    public int Month { get; } = month;
    public int Year { get; } = year;
    public string BudgetType { get; } = budgetType;
}

/// <summary>Published when a financial goal is achieved.</summary>
public sealed class GoalAchievedEvent(int goalId, string goalName) : IEvent
{
    public int GoalId { get; } = goalId;
    public string GoalName { get; } = goalName;
}
