using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.Authentication.Views;

namespace StoreAssistantPro.Modules.Authentication.Services;

public class AuthenticationFlow(
    IServiceProvider serviceProvider,
    IWindowSizingService sizingService) : IAuthenticationFlow
{
    public bool RunFirstTimeSetup()
    {
        var window = serviceProvider.GetRequiredService<SetupWindow>();
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.WindowState = WindowState.Maximized;
        window.ResizeMode = ResizeMode.NoResize;
        return window.ShowDialog() == true;
    }

    public bool TryLogin(out UserType userType)
    {
        userType = default;

        var loginWindow = serviceProvider.GetRequiredService<LoginWindow>();
        sizingService.ConfigureStartupWindow(loginWindow, 480, 820);

        if (loginWindow.ShowDialog() != true)
            return false;

        var vm = (LoginViewModel)loginWindow.DataContext;
        userType = vm.SelectedUserType!.Value;
        return true;
    }
}
