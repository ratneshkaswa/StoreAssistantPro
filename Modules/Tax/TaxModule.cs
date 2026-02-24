using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Tax.Commands;
using StoreAssistantPro.Modules.Tax.Services;
using StoreAssistantPro.Modules.Tax.ViewModels;
using StoreAssistantPro.Modules.Tax.Views;

namespace StoreAssistantPro.Modules.Tax;

public static class TaxModule
{
    public const string TaxManagementDialog = "TaxManagement";

    public static IServiceCollection AddTaxModule(this IServiceCollection services)
    {
        // Services
        services.AddTransient<ITaxService, TaxService>();

        // Command handlers
        services.AddTransient<ICommandRequestHandler<SaveTaxProfileCommand, Unit>, SaveTaxProfileHandler>();
        services.AddTransient<ICommandRequestHandler<ToggleTaxProfileCommand, Unit>, ToggleTaxProfileHandler>();
        services.AddTransient<ICommandRequestHandler<SetDefaultTaxProfileCommand, Unit>, SetDefaultTaxProfileHandler>();

        // ViewModels
        services.AddTransient<TaxManagementViewModel>();

        // Views
        services.AddTransient<TaxManagementWindow>();

        // Dialog registration
        services.AddDialogRegistration<TaxManagementWindow>(TaxManagementDialog);

        return services;
    }
}
