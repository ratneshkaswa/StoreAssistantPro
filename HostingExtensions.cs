using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Commands.Logging;
using StoreAssistantPro.Core.Commands.Offline;
using StoreAssistantPro.Core.Commands.Performance;
using StoreAssistantPro.Core.Commands.Transaction;
using StoreAssistantPro.Core.Commands.Validation;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Features;
using StoreAssistantPro.Core.Intents;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Data;
using StoreAssistantPro.Modules.Authentication;
using StoreAssistantPro.Modules.Billing;
using StoreAssistantPro.Modules.Brands;
using StoreAssistantPro.Modules.Firm;
using StoreAssistantPro.Modules.Inward;
using StoreAssistantPro.Modules.MainShell;
using StoreAssistantPro.Modules.Products;
using StoreAssistantPro.Modules.Sales;
using StoreAssistantPro.Modules.Startup;
using StoreAssistantPro.Modules.Vendors;
using StoreAssistantPro.Modules.SystemSettings;
using StoreAssistantPro.Modules.Tax;
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
        services.AddSingleton<IZeroClickWorkflowService, ZeroClickWorkflowService>();
        services.AddSingleton<ICommandBus, CommandBus>();
        services.AddSingleton<ICommandExecutionPipeline, CommandExecutionPipeline>();
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(LoggingPipelineBehavior<,>));
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(OfflinePipelineBehavior<,>));
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(TransactionPipelineBehavior<,>));
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(PerformancePipelineBehavior<,>));
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<IFocusLockService, FocusLockService>();
        services.AddSingleton<IFeatureToggleService, FeatureToggleService>();
        services.AddTransient<IMasterPinValidator, MasterPinValidator>();
        services.AddSingleton<IWindowSizingService, WindowSizingService>();
        services.AddSingleton<IRegionalSettingsService, RegionalSettingsService>();
        services.AddTransient<ITransactionHelper, TransactionHelper>();
        services.AddTransient<ITransactionSafetyService, TransactionSafetyService>();
        services.AddTransient<IApplicationInfoService, ApplicationInfoService>();
        services.AddSingleton<IPerformanceMonitor, PerformanceMonitor>();
        services.AddSingleton<ITaxCalculationService, TaxCalculationService>();
        services.AddSingleton<IPricingCalculationService, PricingCalculationService>();
        services.AddSingleton<IBillCalculationService, BillCalculationService>();
        services.AddSingleton<IConnectivityMonitorService, ConnectivityMonitorService>();
        services.AddSingleton<IOfflineModeService, OfflineModeService>();
        services.AddSingleton<INotificationService, NotificationService>();
        services.AddSingleton<IToastService, ToastService>();
        services.AddSingleton<IDensityService, DensityService>();
        services.AddSingleton<ICalmUIService, CalmUIService>();
        services.AddSingleton<IFocusRuleEngine, FocusRuleEngine>();
        services.AddSingleton<IPredictiveFocusService, PredictiveFocusService>();
        services.AddSingleton<IFocusSafetyGuard, FocusSafetyGuard>();
        services.AddSingleton<ITipStateService, TipStateService>();
        services.AddSingleton<ITipRegistryService, TipRegistryService>();
        services.AddSingleton<IOnboardingJourneyService, OnboardingJourneyService>();
        services.AddSingleton<IUserInteractionTracker, UserInteractionTracker>();
        services.AddSingleton<IContextHelpService, ContextHelpService>();
        services.AddSingleton<ITipRotationService, TipRotationService>();
        services.AddSingleton<OnboardingTipRegistrar>();
        services.AddSingleton<IIntentDetectionService, IntentDetectionService>();
        services.AddSingleton<IZeroClickPinService, ZeroClickPinService>();
        services.AddSingleton<ISmartEnterKeyService, SmartEnterKeyService>();
        services.AddSingleton<IMicroFeedbackService, MicroFeedbackService>();
        services.AddSingleton<IZeroClickSafetyPolicy, ZeroClickSafetyPolicy>();
        services.AddSingleton<IFlowStateEngine, FlowStateEngine>();
        services.AddSingleton(sp => new Lazy<IFlowStateEngine>(sp.GetRequiredService<IFlowStateEngine>));
        services.AddSingleton<IInteractionTracker, InteractionTracker>();

        return services;
    }

    public static IServiceCollection AddModules(this IServiceCollection services)
    {
        var pageRegistry = new NavigationPageRegistry();
        services.AddSingleton(pageRegistry);

        var focusMapRegistry = new FocusMapRegistry();
        services.AddSingleton<IFocusMapRegistry>(focusMapRegistry);

        services
            .AddStartupModule()
            .AddAuthenticationModule()
            .AddMainShellModule(pageRegistry)
            .AddProductsModule(pageRegistry)
            .AddBrandsModule(pageRegistry)
            .AddSalesModule(pageRegistry)
            .AddVendorsModule(pageRegistry, focusMapRegistry)
            .AddInwardModule(pageRegistry)
            .AddFirmModule()
            .AddUsersModule()
            .AddSystemSettingsModule()
            .AddBillingModule()
            .AddTaxModule();

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
