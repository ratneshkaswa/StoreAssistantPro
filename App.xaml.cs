using System.Diagnostics;
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
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Data;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.Startup.Workflows;

namespace StoreAssistantPro;

public partial class App : Application
{
    private static readonly object CultureSyncRoot = new();
    private static bool _languageMetadataConfigured;
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
        var startupTimestamp = Stopwatch.GetTimestamp();

        try
        {
            await InitializeAndRunAsync(startupTimestamp);
        }
        catch (Exception ex)
        {
            // Last-resort catch — covers DI build failures, host start failures,
            // and anything before the dispatcher handler is wired.
            _logger?.LogCritical(ex, "Fatal error during application startup");
            ShowFatalErrorDialog(ex);
            Shutdown(1);
        }
    }

    private async Task InitializeAndRunAsync(long startupTimestamp)
    {
        // Build DI container from the executable directory so appsettings.json
        // resolves correctly even when launched from shortcuts/other folders.
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });

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

        // Wire static logger factory into BaseViewModel so all
        // RunAsync / RunLoadAsync exception catches are logged.
        Core.BaseViewModel.SetLoggerFactory(
            _host.Services.GetRequiredService<ILoggerFactory>());

        // ── Global exception handlers ────────────────────────────────

        // 1. UI thread exceptions (bindings, commands, event handlers)
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 2. Background thread exceptions (Thread, ThreadPool)
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;

        // 3. Fire-and-forget async Task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        _logger.LogInformation("Application starting");

        // ── Phase 3: Pre-warm EF Core model ──────────────────────────
        var warmupTask = Task.Run(async () =>
        {
            try
            {
                var factory = _host.Services
                    .GetRequiredService<IDbContextFactory<AppDbContext>>();
                await using var _ = await factory.CreateDbContextAsync()
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EF Core warmup failed — migration step will retry");
            }
        });

        await _host.StartAsync();
        await warmupTask;

        var startupElapsed = Stopwatch.GetElapsedTime(startupTimestamp);
        _logger.LogInformation("Application ready in {ElapsedMs:F0} ms", startupElapsed.TotalMilliseconds);

        // ── Phase 4: Startup workflow (migrate, auto-init, load) ────
        var workflowManager = _host.Services.GetRequiredService<IWorkflowManager>();
        var shellFlow = _host.Services.GetRequiredService<IMainShellFlow>();

        await workflowManager.StartWorkflowAsync(StartupWorkflow.WorkflowName);

        if (workflowManager.Context.Has("Error"))
        {
            AppDialogPresenter.ShowError(
                "Database Connection Error",
                $"Cannot connect to the database.\n\nPlease check appsettings.json connection string.\n\n{workflowManager.Context.Get<string>("Error")}");

            Shutdown();
            return;
        }

        // Single-window architecture: show MainWindow once.
        // MainViewModel starts on the login page; shell chrome is hidden until login.
        shellFlow.ShowMainWindow();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        var host = _host;

        try
        {
            _logger?.LogInformation("Application exiting");
            if (host is not null)
                await host.StopAsync(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during host shutdown");
        }
        finally
        {
            host?.Dispose();
            base.OnExit(e);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var errorId = GenerateErrorId();
        _logger?.LogError(e.Exception, "Unhandled UI thread exception [{ErrorId}]", errorId);
        ShowErrorDialog(e.Exception, errorId);
        e.Handled = true;
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            var errorId = GenerateErrorId();
            _logger?.LogCritical(ex, "Unhandled background thread exception [{ErrorId}] (IsTerminating={IsTerminating})", errorId, e.IsTerminating);
            Dispatcher.BeginInvoke(() => ShowErrorDialog(ex, errorId));
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        var errorId = GenerateErrorId();
        _logger?.LogError(e.Exception, "Unobserved Task exception [{ErrorId}]", errorId);
        e.SetObserved();
        Dispatcher.BeginInvoke(() => ShowErrorDialog(e.Exception.InnerException ?? e.Exception, errorId));
    }

    private static void ShowErrorDialog(Exception ex, string errorId)
    {
        var (title, userMessage) = CategorizeException(ex);

        AppDialogPresenter.ShowError(
            title,
            $"{userMessage}\n\nError Reference: {errorId}\n\nThe error has been logged. Please contact support if this persists.");
    }

    private static void ShowFatalErrorDialog(Exception ex)
    {
        var (title, userMessage) = CategorizeException(ex);

        AppDialogPresenter.ShowError(
            "Fatal Error",
            $"The application failed to start.\n\n{title}: {userMessage}\n\nPlease check your configuration and try again.");
    }

    private static (string Title, string Message) CategorizeException(Exception ex) => ex switch
    {
        Microsoft.Data.SqlClient.SqlException sqlEx =>
            ("Database Error", $"A database error occurred: {sqlEx.Message}"),
        DbUpdateException dbEx =>
            ("Data Error", $"Failed to save data: {(dbEx.InnerException ?? dbEx).Message}"),
        InvalidOperationException invOp when invOp.Message.Contains("connection string", StringComparison.OrdinalIgnoreCase) =>
            ("Configuration Error", "Database connection string is missing or invalid. Check appsettings.json."),
        TimeoutException =>
            ("Timeout", "The operation timed out. Please check your database connection and try again."),
        OperationCanceledException =>
            ("Cancelled", "The operation was cancelled."),
        _ =>
            ("Unexpected Error", $"An unexpected error occurred: {ex.Message}")
    };

    private static string GenerateErrorId() =>
        $"ERR-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

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
        lock (CultureSyncRoot)
        {
            if (_languageMetadataConfigured)
                return;

            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

            _languageMetadataConfigured = true;
        }
    }
}
