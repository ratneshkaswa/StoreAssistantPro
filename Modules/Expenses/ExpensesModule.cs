using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Expenses.Services;
using StoreAssistantPro.Modules.Expenses.ViewModels;

namespace StoreAssistantPro.Modules.Expenses;

public static class ExpensesModule
{
    public const string ExpenseManagementPage = "ExpenseManagement";

    public static IServiceCollection AddExpensesModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<ExpenseManagementViewModel>(ExpenseManagementPage)
            .RequireFeature(ExpenseManagementPage, FeatureFlags.Expenses);
        services.AddTransient<IExpenseService, ExpenseService>();
        services.AddTransient<ExpenseManagementViewModel>();
        return services;
    }
}
