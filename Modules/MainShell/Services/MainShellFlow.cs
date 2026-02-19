using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.MainShell.ViewModels;
using StoreAssistantPro.Modules.MainShell.Views;

namespace StoreAssistantPro.Modules.MainShell.Services;

public class MainShellFlow(IServiceProvider serviceProvider) : IMainShellFlow
{
    public bool ShowMainWindow()
    {
        var mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        var mainVm = serviceProvider.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainVm;
        mainWindow.ShowDialog();
        return mainVm.IsLoggingOut;
    }
}
