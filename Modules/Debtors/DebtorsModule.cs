using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Debtors.Services;
using StoreAssistantPro.Modules.Debtors.ViewModels;

namespace StoreAssistantPro.Modules.Debtors;

public static class DebtorsModule
{
    public const string DebtorManagementPage = "DebtorManagement";

    public static IServiceCollection AddDebtorsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<DebtorManagementViewModel>(DebtorManagementPage)
            .RequireFeature(DebtorManagementPage, FeatureFlags.Debtors);
        pageRegistry.CachePage(DebtorManagementPage);
        services.AddTransient<IDebtorService, DebtorService>();
        services.AddTransient<DebtorManagementViewModel>();
        return services;
    }
}
