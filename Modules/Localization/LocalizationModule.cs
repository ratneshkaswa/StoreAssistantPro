using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Localization.Services;

namespace StoreAssistantPro.Modules.Localization;

public static class LocalizationModule
{
    public static IServiceCollection AddLocalizationModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IIndianNumberFormatService, IndianNumberFormatService>();
        services.AddTransient<IRegionalCalendarService, RegionalCalendarService>();
        services.AddTransient<IStateTaxLabelService, StateTaxLabelService>();
        services.AddTransient<IRegionalReceiptService, RegionalReceiptService>();
        return services;
    }
}
