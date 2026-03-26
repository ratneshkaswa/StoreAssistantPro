using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.NicheVertical.Services;

namespace StoreAssistantPro.Modules.NicheVertical;

public static class NicheVerticalModule
{
    public static IServiceCollection AddNicheVerticalModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IAlterationService, AlterationService>();
        services.AddTransient<IRentalService, RentalService>();
        services.AddTransient<IWholesaleService, WholesaleService>();
        services.AddTransient<IConsignmentService, ConsignmentService>();
        services.AddTransient<ISeasonService, SeasonService>();
        services.AddTransient<IGiftCardService, GiftCardService>();
        services.AddTransient<ILoyaltyService, LoyaltyService>();
        return services;
    }
}
