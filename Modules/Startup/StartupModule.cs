using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Modules.Startup.Services;
using StoreAssistantPro.Modules.Startup.ViewModels;
using StoreAssistantPro.Modules.Startup.Views;
using StoreAssistantPro.Modules.Startup.Workflows;

namespace StoreAssistantPro.Modules.Startup;

public static class StartupModule
{
    public static IServiceCollection AddStartupModule(this IServiceCollection services)
    {
        services.AddTransient<IStartupService, StartupService>();
        services.AddSingleton<IWorkflow, StartupWorkflow>();
        services.AddSingleton<ISetupWizardFlow, SetupWizardFlow>();
        services.AddTransient<SetupWizardViewModel>();
        services.AddTransient<SetupWizardWindow>();

        return services;
    }
}
