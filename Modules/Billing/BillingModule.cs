using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Commands.Validation;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Modules.Billing.Commands;
using StoreAssistantPro.Modules.Billing.Services;
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
        // Services
        services.AddSingleton<IBillingModeService, BillingModeService>();
        services.AddSingleton<IBillingSessionService, BillingSessionService>();
        services.AddSingleton<ISmartBillingModeService, SmartBillingModeService>();
        services.AddSingleton<IBillingSessionPersistenceService, BillingSessionPersistenceService>();
        services.AddSingleton<IBillingAutoSaveService, BillingAutoSaveService>();
        services.AddSingleton<IBillingResumeService, BillingResumeService>();
        services.AddSingleton<IBillingSessionRestoreService, BillingSessionRestoreService>();
        services.AddSingleton<IStaleBillingSessionCleanupService, StaleBillingSessionCleanupService>();
        services.AddSingleton<IBillingSaveLockService, BillingSaveLockService>();

        // Command handlers (pipeline-aware)
        services.AddTransient<ICommandRequestHandler<SaveBillCommand, int>, SaveBillCommandHandler>();

        // Command validators
        services.AddTransient<ICommandValidator<SaveBillCommand>, SaveBillCommandValidator>();

        // Workflows
        services.AddSingleton<IWorkflow, BillingWorkflow>();

        return services;
    }
}
