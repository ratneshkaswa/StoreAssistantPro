using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Modules.Authentication.Workflows;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.Startup.Workflows;

namespace StoreAssistantPro;

public partial class App : Application
{
    private readonly IHost _host;
    private ILogger<App>? _logger;

    public App()
    {
        SetIndianCulture();

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
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        _logger = _host.Services.GetRequiredService<ILogger<App>>();

        // 1. UI thread exceptions (bindings, commands, event handlers)
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 2. Background thread exceptions (Thread, ThreadPool)
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        // 3. Fire-and-forget async Task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _logger.LogInformation("Application starting");

        await _host.StartAsync();

        var workflowManager = _host.Services.GetRequiredService<IWorkflowManager>();
        var session = _host.Services.GetRequiredService<ISessionService>();
        var shellFlow = _host.Services.GetRequiredService<IMainShellFlow>();

        // 1. Startup workflow — migrate DB, first-time setup, load firm
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

        // 2. Main app loop — login ? main window ? logout
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
        _logger?.LogInformation("Application exiting");
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
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
            "Store Assistant Pro — Error",
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
