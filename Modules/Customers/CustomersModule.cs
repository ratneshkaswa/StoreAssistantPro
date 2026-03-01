using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Customers.Commands;
using StoreAssistantPro.Modules.Customers.Services;
using StoreAssistantPro.Modules.Customers.ViewModels;

namespace StoreAssistantPro.Modules.Customers;

public static class CustomersModule
{
    public const string CustomersPage = "Customers";

    public static IServiceCollection AddCustomersModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<CustomersViewModel>(CustomersPage);

        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddSingleton<CustomerStatsListener>();
        services.AddTransient<ICommandRequestHandler<SaveCustomerCommand, Unit>, SaveCustomerHandler>();
        services.AddTransient<ICommandRequestHandler<DeleteCustomerCommand, Unit>, DeleteCustomerHandler>();
        services.AddTransient<CustomersViewModel>();

        return services;
    }
}
