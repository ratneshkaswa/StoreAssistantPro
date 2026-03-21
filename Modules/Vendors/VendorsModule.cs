using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Vendors.Services;
using StoreAssistantPro.Modules.Vendors.ViewModels;

namespace StoreAssistantPro.Modules.Vendors;

public static class VendorsModule
{
    public const string VendorManagementPage = "VendorManagement";

    public static IServiceCollection AddVendorsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<VendorManagementViewModel>(VendorManagementPage)
            .RequireFeature(VendorManagementPage, FeatureFlags.VendorManagement);
        services.AddTransient<IVendorService, VendorService>();
        services.AddTransient<IVendorLedgerService, VendorLedgerService>();
        services.AddTransient<VendorManagementViewModel>();
        return services;
    }
}
