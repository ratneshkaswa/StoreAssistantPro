using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;
using StoreAssistantPro.Modules.Billing.Views;

namespace StoreAssistantPro.Modules.Billing;

public static class BillingModule
{
    public const string BillingDialog = "Billing";

    public static IServiceCollection AddBillingModule(this IServiceCollection services)
    {
        services.AddTransient<IBillingService, BillingService>();
        services.AddTransient<BillingViewModel>();
        services.AddTransient<BillingWindow>();
        services.AddDialogRegistration<BillingWindow>(BillingDialog);
        return services;
    }
}
