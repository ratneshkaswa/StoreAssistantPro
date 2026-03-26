using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.PaymentGateway.Services;

namespace StoreAssistantPro.Modules.PaymentGateway;

public static class PaymentGatewayModule
{
    public static IServiceCollection AddPaymentGatewayModule(this IServiceCollection services)
    {
        // DB-accessing services with no mutable state → Transient.
        services.AddTransient<IPaymentGatewayService, PaymentGatewayService>();
        services.AddTransient<IPaymentReconciliationService, PaymentReconciliationService>();
        services.AddTransient<IPaymentScheduleService, PaymentScheduleService>();
        return services;
    }
}
