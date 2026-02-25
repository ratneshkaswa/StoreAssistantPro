using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Suppliers.Services;
using StoreAssistantPro.Modules.Suppliers.ViewModels;

namespace StoreAssistantPro.Modules.Suppliers;

public static class SuppliersModule
{
    public const string SuppliersPage = "Suppliers";

    public static IServiceCollection AddSuppliersModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<SuppliersViewModel>(SuppliersPage);

        services.AddTransient<ISupplierService, SupplierService>();
        services.AddTransient<SuppliersViewModel>();

        return services;
    }
}
