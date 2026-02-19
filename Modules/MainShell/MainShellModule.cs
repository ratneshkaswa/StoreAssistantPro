using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.MainShell.Views;

namespace StoreAssistantPro.Modules.MainShell;

public static class MainShellModule
{
    public const string DashboardPage = "Dashboard";

    public static IServiceCollection AddMainShellModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        // Page registration (string-key → ViewModel mapping)
        pageRegistry.Map<DashboardViewModel>(DashboardPage);

        // Services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IDashboardService, DashboardService>();
        services.AddSingleton<IMainShellFlow, MainShellFlow>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        return services;
    }
}
