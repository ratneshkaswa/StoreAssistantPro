using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Vendors.Services;

namespace StoreAssistantPro.Modules.Vendors;

public static class VendorsModule
{
    public static IServiceCollection AddVendorsModule(this IServiceCollection services)
    {
        services.AddTransient<IVendorService, VendorService>();
        return services;
    }
}
