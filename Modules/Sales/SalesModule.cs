using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.Sales.Commands;
using StoreAssistantPro.Modules.Sales.Services;
using StoreAssistantPro.Modules.Sales.ViewModels;

namespace StoreAssistantPro.Modules.Sales;

public static class SalesModule
{
    public const string SalesPage = "Sales";

    public static IServiceCollection AddSalesModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        // Page registration
        pageRegistry.Map<SalesViewModel>(SalesPage);
        pageRegistry.RequireFeature(SalesPage, FeatureFlags.Sales);

        // Services
        services.AddTransient<ISalesService, SalesService>();
        services.AddSingleton<IOfflineBillingQueue, OfflineBillingQueue>();
        services.AddSingleton<IOfflineSyncService, OfflineSyncService>();

        // Command handlers
        services.AddTransient<ICommandRequestHandler<CompleteSaleCommand, Unit>, CompleteSaleHandler>();

        // ViewModels
        services.AddTransient<SalesViewModel>();

        return services;
    }
}
