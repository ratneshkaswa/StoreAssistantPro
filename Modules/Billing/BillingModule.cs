using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Modules.Billing.Workflows;

namespace StoreAssistantPro.Modules.Billing;

/// <summary>
/// Future billing module placeholder.
/// <para>
/// Planned components:
/// <list type="bullet">
///   <item><b>Services:</b> IBillingService — cart management, receipt generation, payment processing.</item>
///   <item><b>ViewModels:</b> BillingViewModel, CartViewModel, ReceiptViewModel.</item>
///   <item><b>Views:</b> BillingView (POS terminal), CartPanel, ReceiptPreview.</item>
///   <item><b>Models:</b> BillingSession, CartItem, Receipt, PaymentTransaction.</item>
/// </list>
/// </para>
/// </summary>
public static class BillingModule
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services)
    {
        // Workflows
        services.AddSingleton<IWorkflow, BillingWorkflow>();

        // TODO: Register billing services, ViewModels, and Views when implemented.
        // services.AddSingleton<IBillingService, BillingService>();
        // services.AddTransient<BillingViewModel>();

        return services;
    }
}
