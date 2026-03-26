using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.MultiCountry.Services;

namespace StoreAssistantPro.Modules.MultiCountry;

public static class MultiCountryModule
{
    public static IServiceCollection AddMultiCountryModule(this IServiceCollection services)
    {
        // Currency and country profiles are cached in-memory → Singleton.
        services.AddSingleton<ICurrencyService, CurrencyService>();
        services.AddSingleton<ICountryProfileService, CountryProfileService>();
        return services;
    }
}
