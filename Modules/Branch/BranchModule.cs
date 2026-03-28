using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Branch.Services;
using StoreAssistantPro.Modules.Branch.ViewModels;

namespace StoreAssistantPro.Modules.Branch;

public static class BranchModule
{
    public const string BranchManagementPage = "BranchManagement";

    public static IServiceCollection AddBranchModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<BranchManagementViewModel>(BranchManagementPage)
            .RequireFeature(BranchManagementPage, FeatureFlags.Branch);
        pageRegistry.CachePage(BranchManagementPage);
        services.AddTransient<IBranchBillService, BranchBillService>();
        services.AddTransient<BranchManagementViewModel>();
        return services;
    }
}
