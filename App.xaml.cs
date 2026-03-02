using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Data;
using StoreAssistantPro.Modules.Authentication.Workflows;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.Startup.Workflows;

namespace StoreAssistantPro;

public partial class App : Application
{
    private IHost _host = null!;
    private ILogger<App>? _logger;

    /// <summary>
    /// Exposes the DI container for attached behaviors that cannot use constructor injection.
    /// </summary>
    public IServiceProvider? Services => _host?.Services;

    public App()
    {
        SetIndianCulture();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Build DI container
        var builder = Host.CreateApplicationBuilder();

        builder.Services
            .AddDataAccess(builder.Configuration)
            .AddCoreServices()
            .AddModules();

        _host = builder.Build();

        // Wire up string-key registrations collected during DI setup
        var pageRegistry = _host.Services.GetRequiredService<NavigationPageRegistry>();
        pageRegistry.ApplyTo(_host.Services.GetRequiredService<INavigationService>());

        _host.Services.ApplyDialogRegistrations();

        // Eagerly resolve the focus lock service so it subscribes
        // to mode-changed events before any mode transition occurs.
        _host.Services.GetRequiredService<IFocusLockService>();

        // Eagerly resolve the onboarding tip registrar so all first-time
        // tips are registered before any view loads.
        _host.Services.GetRequiredService<OnboardingTipRegistrar>();

        _logger = _host.Services.GetRequiredService<ILogger<App>>();

        // 1. UI thread exceptions (bindings, commands, event handlers)
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 2. Background thread exceptions (Thread, ThreadPool)
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        // 3. Fire-and-forget async Task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _logger.LogInformation("Application starting");

        // ?? Phase 3: Pre-warm EF Core model on a background thread ??
        // The first CreateDbContextAsync compiles the EF model (~500 ms
        // cold start).  Kick it off in parallel with host startup so
        // the migration step sees an already-warm factory.
        var warmupTask = Task.Run(async () =>
        {
            var factory = _host.Services
                .GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var _ = await factory.CreateDbContextAsync()
                .ConfigureAwait(false);
        });

        await _host.StartAsync();
        await warmupTask;

        // ?? Phase 4: Startup workflow (migrate, setup, load) ????????
        var workflowManager = _host.Services.GetRequiredService<IWorkflowManager>();
        var session = _host.Services.GetRequiredService<ISessionService>();
        var shellFlow = _host.Services.GetRequiredService<IMainShellFlow>();

        await workflowManager.StartWorkflowAsync(StartupWorkflow.WorkflowName);

        if (workflowManager.Context.Has("Error"))
        {
            MessageBox.Show(
                $"Cannot connect to the database.\n\nPlease check appsettings.json connection string.\n\n{workflowManager.Context.Get<string>("Error")}",
                "Database Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
            return;
        }

        // If setup was cancelled (user closed the One Time Setup window),
        // shut down the application � do not proceed to login.
        if (!workflowManager.Context.Get<bool>("IsInitialized")
            && !await _host.Services.GetRequiredService<StoreAssistantPro.Modules.Startup.Services.IStartupService>().IsAppInitializedAsync())
        {
            Shutdown();
            return;
        }

        // Main app loop — login → main window → logout
        while (true)
        {
            await workflowManager.StartWorkflowAsync(LoginWorkflow.WorkflowName);

            if (!session.IsLoggedIn)
            {
                Shutdown();
                return;
            }

            // Show main window (blocks until closed)
            if (!shellFlow.ShowMainWindow())
            {
                Shutdown();
                return;
            }

            // LogoutCommand already cleared session + AppState before window closed
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            _logger?.LogInformation("Application exiting");
            await _host.StopAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during host shutdown");
        }
        finally
        {
            _host.Dispose();
            base.OnExit(e);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unhandled UI thread exception");
        ShowErrorDialog(e.Exception);
        e.Handled = true;
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            _logger?.LogCritical(ex, "Unhandled background thread exception (IsTerminating={IsTerminating})", e.IsTerminating);
            Dispatcher.Invoke(() => ShowErrorDialog(ex));
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Unobserved Task exception");
        e.SetObserved();
        Dispatcher.Invoke(() => ShowErrorDialog(e.Exception.InnerException ?? e.Exception));
    }

    private static void ShowErrorDialog(Exception ex)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{ex.Message}\n\nThe error has been logged. Please contact support if this persists.",
            "Store Assistant Pro � Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void SetIndianCulture()
    {
        var culture = new CultureInfo("en-IN");

        // Current thread (UI thread)
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        // All future threads (Task.Run, ThreadPool, async continuations)
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        // WPF XAML bindings (StringFormat, converters, etc.)
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(
                XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }
}
