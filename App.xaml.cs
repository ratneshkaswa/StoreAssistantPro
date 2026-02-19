using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Services;
using StoreAssistantPro.Session;
using StoreAssistantPro.ViewModels;
using StoreAssistantPro.Views;

namespace StoreAssistantPro;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services
            .AddDataAccess(builder.Configuration)
            .AddApplicationServices()
            .AddViewModels()
            .AddViews();

        _host = builder.Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        await _host.StartAsync();

        try
        {
            var dbFactory = _host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using (var db = await dbFactory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Cannot connect to the database.\n\nPlease check appsettings.json connection string.\n\n{ex.Message}",
                "Database Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown();
            return;
        }

        // 1. First-time setup
        var startupService = _host.Services.GetRequiredService<IStartupService>();
        if (!await startupService.IsAppInitializedAsync())
        {
            var setupWindow = _host.Services.GetRequiredService<FirstTimeSetupWindow>();
            if (setupWindow.ShowDialog() != true)
            {
                Shutdown();
                return;
            }
        }

        // 2. Main app loop — supports logout back to user selection
        var session = _host.Services.GetRequiredService<ISessionService>();

        while (true)
        {
            // Login flow
            if (!ShowLoginFlow(out var userType))
            {
                Shutdown();
                return;
            }

            await session.LoginAsync(userType);

            // Show main window (blocks until closed)
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            var mainVm = _host.Services.GetRequiredService<MainViewModel>();
            mainWindow.DataContext = mainVm;
            mainWindow.ShowDialog();

            // Check if user logged out or closed the window
            if (!mainVm.IsLoggingOut)
            {
                Shutdown();
                return;
            }

            session.Logout();
        }
    }

    private bool ShowLoginFlow(out UserType userType)
    {
        userType = default;

        while (true)
        {
            var selectionWindow = _host.Services.GetRequiredService<UserSelectionWindow>();
            if (selectionWindow.ShowDialog() != true)
                return false;

            var selectedType = ((UserSelectionViewModel)selectionWindow.DataContext).SelectedUserType;

            var pinWindow = _host.Services.GetRequiredService<PinLoginWindow>();
            ((PinLoginViewModel)pinWindow.DataContext).UserType = selectedType;

            if (pinWindow.ShowDialog() == true)
            {
                userType = selectedType;
                return true;
            }
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
