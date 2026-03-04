using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Tax.Services;
using StoreAssistantPro.Modules.Tax.ViewModels;
using StoreAssistantPro.Modules.Tax.Views;

namespace StoreAssistantPro.Modules.Tax;

public static class TaxModule
{
    public const string TaxManagementDialog = "TaxManagement";

    public static IServiceCollection AddTaxModule(this IServiceCollection services)
    {
        services.AddTransient<ITaxService, TaxService>();
        services.AddTransient<TaxManagementViewModel>();
        services.AddTransient<TaxManagementWindow>();
        services.AddDialogRegistration<TaxManagementWindow>(TaxManagementDialog);
        return services;
    }
}
