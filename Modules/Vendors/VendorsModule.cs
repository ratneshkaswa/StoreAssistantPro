using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Vendors.Services;
using StoreAssistantPro.Modules.Vendors.ViewModels;
using StoreAssistantPro.Modules.Vendors.Views;

namespace StoreAssistantPro.Modules.Vendors;

public static class VendorsModule
{
    public const string VendorManagementDialog = "VendorManagement";

    public static IServiceCollection AddVendorsModule(this IServiceCollection services)
    {
        services.AddTransient<IVendorService, VendorService>();
        services.AddTransient<VendorManagementViewModel>();
        services.AddTransient<VendorManagementWindow>();
        services.AddDialogRegistration<VendorManagementWindow>(VendorManagementDialog);
        return services;
    }
}
