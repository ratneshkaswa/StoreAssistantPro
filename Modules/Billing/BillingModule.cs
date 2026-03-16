using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;

namespace StoreAssistantPro.Modules.Billing;

public static class BillingModule
{
    public const string BillingPage = "Billing";
    public const string SaleHistoryPage = "SaleHistory";

    public static IServiceCollection AddBillingModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<BillingViewModel>(BillingPage)
            .RequireFeature(BillingPage, FeatureFlags.Billing);
        pageRegistry.Map<SaleHistoryViewModel>(SaleHistoryPage)
            .RequireFeature(SaleHistoryPage, FeatureFlags.SaleHistory);
        services.AddTransient<IBillingService, BillingService>();
        services.AddTransient<IReceiptService, ReceiptService>();
        services.AddTransient<ISaleHistoryService, SaleHistoryService>();
        services.AddTransient<ISaleReturnService, SaleReturnService>();
        services.AddTransient<BillingViewModel>();
        services.AddTransient<SaleHistoryViewModel>();
        return services;
    }
}
