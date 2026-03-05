using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;
using StoreAssistantPro.Modules.Billing.Views;

namespace StoreAssistantPro.Modules.Billing;

public static class BillingModule
{
    public const string BillingDialog = "Billing";
    public const string SaleHistoryDialog = "SaleHistory";

    public static IServiceCollection AddBillingModule(this IServiceCollection services)
    {
        services.AddTransient<IBillingService, BillingService>();
        services.AddTransient<IReceiptService, ReceiptService>();
        services.AddTransient<ISaleHistoryService, SaleHistoryService>();
        services.AddTransient<BillingViewModel>();
        services.AddTransient<SaleHistoryViewModel>();
        services.AddTransient<BillingWindow>();
        services.AddTransient<SaleHistoryWindow>();
        services.AddDialogRegistration<BillingWindow>(BillingDialog);
        services.AddDialogRegistration<SaleHistoryWindow>(SaleHistoryDialog);
        return services;
    }
}
