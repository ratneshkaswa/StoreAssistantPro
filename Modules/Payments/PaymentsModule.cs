using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Payments.Services;
using StoreAssistantPro.Modules.Payments.ViewModels;

namespace StoreAssistantPro.Modules.Payments;

public static class PaymentsModule
{
    public const string PaymentManagementPage = "PaymentManagement";

    public static IServiceCollection AddPaymentsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        pageRegistry.Map<PaymentManagementViewModel>(PaymentManagementPage)
            .RequireFeature(PaymentManagementPage, FeatureFlags.Payments);
        pageRegistry.CachePage(PaymentManagementPage);
        services.AddTransient<IPaymentService, PaymentService>();
        services.AddTransient<PaymentManagementViewModel>();
        return services;
    }
}
