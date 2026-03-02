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
        sizingService.ConfigureStartupWindow(window, 520, 1020);
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
