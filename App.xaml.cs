using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

    public App()
    {
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
        DispatcherUnhandledException += OnDispatcherUnhandledException;

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

            session.Logout();
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        e.Handled = true;
    }
}
