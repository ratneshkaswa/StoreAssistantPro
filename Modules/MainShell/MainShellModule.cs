using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.MainShell.Views;

namespace StoreAssistantPro.Modules.MainShell;

public static class MainShellModule
{
    public const string MainWorkspacePage = "MainWorkspace";

    public static IServiceCollection AddMainShellModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry)
    {
        // Page registration (string-key → ViewModel mapping)
        pageRegistry.Map<WorkspaceViewModel>(MainWorkspacePage);

        // Services
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IQuickActionService, QuickActionService>();
        services.AddSingleton<IShortcutService, ShortcutService>();
        services.AddTransient<IDashboardService, DashboardService>();
        services.AddSingleton<IMainShellFlow, MainShellFlow>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<WorkspaceViewModel>();

        // Views
        services.AddTransient<MainWindow>();

        return services;
    }
}
