using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Customers.Commands;
using StoreAssistantPro.Modules.Customers.Services;

namespace StoreAssistantPro.Modules.Customers;

public static class CustomersModule
{
    public static IServiceCollection AddCustomersModule(this IServiceCollection services)
    {
        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddTransient<ICommandRequestHandler<SaveCustomerCommand, Unit>, SaveCustomerHandler>();
        services.AddTransient<ICommandRequestHandler<DeleteCustomerCommand, Unit>, DeleteCustomerHandler>();

        return services;
    }
}
