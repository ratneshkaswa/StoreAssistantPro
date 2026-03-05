using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Customers.Services;
using StoreAssistantPro.Modules.Customers.ViewModels;
using StoreAssistantPro.Modules.Customers.Views;

namespace StoreAssistantPro.Modules.Customers;

public static class CustomersModule
{
    public const string CustomerManagementDialog = "CustomerManagement";

    public static IServiceCollection AddCustomersModule(this IServiceCollection services)
    {
        services.AddTransient<ICustomerService, CustomerService>();
        services.AddTransient<CustomerManagementViewModel>();
        services.AddTransient<CustomerManagementWindow>();
        services.AddDialogRegistration<CustomerManagementWindow>(CustomerManagementDialog);
        return services;
    }
}
