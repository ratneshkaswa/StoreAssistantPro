using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Quotations.Services;
using StoreAssistantPro.Modules.Quotations.ViewModels;

namespace StoreAssistantPro.Modules.Quotations;

public static class QuotationsModule
{
    public const string QuotationsPage = "Quotations";

    public static IServiceCollection AddQuotationsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<QuotationViewModel>(QuotationsPage)
            .RequireFeature(QuotationsPage, FeatureFlags.Quotations);
        services.AddTransient<IQuotationService, QuotationService>();
        services.AddTransient<QuotationViewModel>();
        return services;
    }
}
