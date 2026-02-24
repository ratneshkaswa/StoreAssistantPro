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
        var window = serviceProvider.GetRequiredService<FirstTimeSetupWindow>();
        sizingService.ConfigureStartupWindow(window, 460, 560);
        return window.ShowDialog() == true;
    }

    public bool TryLogin(out UserType userType)
    {
        userType = default;

        var loginWindow = serviceProvider.GetRequiredService<UnifiedLoginWindow>();
        sizingService.ConfigureStartupWindow(loginWindow, 420, 600);

        if (loginWindow.ShowDialog() != true)
            return false;

        var vm = (UnifiedLoginViewModel)loginWindow.DataContext;
        userType = vm.SelectedUserType!.Value;
        return true;
    }
}
