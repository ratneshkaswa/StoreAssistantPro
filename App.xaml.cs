using System.Windows;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StoreAssistantPro.Data;
using StoreAssistantPro.Services;
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
        await _host.StartAsync();

        var dbFactory = _host.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using (var db = await dbFactory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 1. First-time setup check
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

        // 2. Login flow (loops until successful login or user exits)
        if (!ShowLoginFlow())
        {
            Shutdown();
            return;
        }

        // 3. Main application
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    private bool ShowLoginFlow()
    {
        while (true)
        {
            var selectionWindow = _host.Services.GetRequiredService<UserSelectionWindow>();
            if (selectionWindow.ShowDialog() != true)
                return false;

            var selectedType = ((UserSelectionViewModel)selectionWindow.DataContext).SelectedUserType;

            var pinWindow = _host.Services.GetRequiredService<PinLoginWindow>();
            ((PinLoginViewModel)pinWindow.DataContext).UserType = selectedType;

            if (pinWindow.ShowDialog() == true)
                return true;
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
