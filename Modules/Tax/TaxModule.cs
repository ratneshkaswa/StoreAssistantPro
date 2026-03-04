using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Tax.Services;

namespace StoreAssistantPro.Modules.Tax;

public static class TaxModule
{
    public static IServiceCollection AddTaxModule(this IServiceCollection services)
    {
        services.AddTransient<ITaxService, TaxService>();
        return services;
    }
}
