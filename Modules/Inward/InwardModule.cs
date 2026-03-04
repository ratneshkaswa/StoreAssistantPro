using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Inward.Services;
using StoreAssistantPro.Modules.Inward.ViewModels;
using StoreAssistantPro.Modules.Inward.Views;

namespace StoreAssistantPro.Modules.Inward;

public static class InwardModule
{
    public const string InwardEntryDialog = "InwardEntry";

    public static IServiceCollection AddInwardModule(this IServiceCollection services)
    {
        services.AddTransient<IInwardService, InwardService>();
        services.AddTransient<InwardEntryViewModel>();
        services.AddTransient<InwardEntryWindow>();
        services.AddDialogRegistration<InwardEntryWindow>(InwardEntryDialog);
        return services;
    }
}
