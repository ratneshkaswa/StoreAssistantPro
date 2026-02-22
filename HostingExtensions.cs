using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Data;
using StoreAssistantPro.Modules.Authentication;
using StoreAssistantPro.Modules.Billing;
using StoreAssistantPro.Modules.Firm;
using StoreAssistantPro.Modules.MainShell;
using StoreAssistantPro.Modules.Products;
using StoreAssistantPro.Modules.Sales;
using StoreAssistantPro.Modules.Startup;
using StoreAssistantPro.Modules.SystemSettings;
using StoreAssistantPro.Modules.Users;

namespace StoreAssistantPro;

internal static class HostingExtensions
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, IConfiguration configuration)
    {
        var isDevelopment = false;
#if DEBUG
        isDevelopment = true;
#endif

        services.AddPooledDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions
                        .EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null)
                        .CommandTimeout(30))
                .EnableDetailedErrors(isDevelopment)
                .EnableSensitiveDataLogging(false));

        return services;
    }

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddProvider(new FileLoggerProvider());
        });

        services.AddSingleton<IAppStateService, AppStateService>();
        services.AddSingleton<IStatusBarService, StatusBarService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<ISessionService, SessionService>();
        services.AddSingleton<IWindowRegistry, WindowRegistry>();
        services.AddSingleton<IWorkflowManager, WorkflowManager>();
        services.AddSingleton<ICommandBus, CommandBus>();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<IFeatureToggleService, FeatureToggleService>();
        services.AddTransient<IMasterPinValidator, MasterPinValidator>();
        services.AddSingleton<IWindowSizingService, WindowSizingService>();
        services.AddSingleton<IRegionalSettingsService, RegionalSettingsService>();
        services.AddTransient<ITransactionHelper, TransactionHelper>();
        services.AddTransient<IApplicationInfoService, ApplicationInfoService>();
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();

        return services;
    }

    public static IServiceCollection AddModules(this IServiceCollection services)
    {
        var pageRegistry = new NavigationPageRegistry();
        services.AddSingleton(pageRegistry);

        services
            .AddStartupModule()
            .AddAuthenticationModule()
            .AddMainShellModule(pageRegistry)
            .AddProductsModule(pageRegistry)
            .AddSalesModule(pageRegistry)
            .AddFirmModule()
            .AddUsersModule()
            .AddSystemSettingsModule()
            .AddBillingModule();

        return services;
    }

    /// <summary>
    /// Registers a deferred dialog-key mapping. After the host is built and
    /// <see cref="IWindowRegistry"/> is resolved, call
    /// <see cref="ApplyDialogRegistrations"/> to push all mappings into it.
    /// </summary>
    public static IServiceCollection AddDialogRegistration<TWindow>(
        this IServiceCollection services, string dialogKey) where TWindow : Window
    {
        services.AddSingleton(new DialogRegistration(dialogKey, typeof(TWindow)));
        return services;
    }

    /// <summary>
    /// Pushes all <see cref="DialogRegistration"/> entries collected during DI
    /// setup into the <see cref="IWindowRegistry"/>.
    /// </summary>
    public static void ApplyDialogRegistrations(this IServiceProvider services)
    {
        var registry = services.GetRequiredService<IWindowRegistry>();
        foreach (var entry in services.GetServices<DialogRegistration>())
        {
            var method = typeof(IWindowRegistry)
                .GetMethod(nameof(IWindowRegistry.RegisterDialog))!
                .MakeGenericMethod(entry.WindowType);
            method.Invoke(registry, [entry.DialogKey]);
        }
    }
}

internal sealed record DialogRegistration(string DialogKey, Type WindowType);
