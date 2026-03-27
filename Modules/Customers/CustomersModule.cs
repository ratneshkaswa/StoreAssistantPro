using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Customers.Services;
using StoreAssistantPro.Modules.Customers.ViewModels;

namespace StoreAssistantPro.Modules.Customers;

public static class CustomersModule
{
    public const string CustomerManagementPage = "CustomerManagement";

    public static IServiceCollection AddCustomersModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<CustomerManagementViewModel>(CustomerManagementPage)
            .RequireFeature(CustomerManagementPage, FeatureFlags.Customers)
            .CachePage(CustomerManagementPage);
        services.AddTransient<ICustomerService, CustomerService>();
        services.AddTransient<CustomerManagementViewModel>();
        return services;
    }
}
