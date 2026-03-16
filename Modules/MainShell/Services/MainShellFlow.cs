using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.MainShell.Views;

namespace StoreAssistantPro.Modules.MainShell.Services;

public class MainShellFlow(
    IServiceProvider serviceProvider,
    ILogger<MainShellFlow> logger) : IMainShellFlow
{
    public void ShowMainWindow()
    {
        try
        {
            var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
            var mainVm = serviceProvider.GetRequiredService<MainViewModel>();
            mainWindow.DataContext = mainVm;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open main window");
            AppDialogPresenter.ShowError(
                "Unable to Open Window",
                "The main window could not be opened.\n\nThe error has been logged.");
        }
    }
}
