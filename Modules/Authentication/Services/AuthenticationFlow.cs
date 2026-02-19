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
        sizingService.ConfigureStartupWindow(window, 400, 480);
        return window.ShowDialog() == true;
    }

    public bool TryLogin(out UserType userType)
    {
        userType = default;

        while (true)
        {
            var selectionWindow = serviceProvider.GetRequiredService<UserSelectionWindow>();
            sizingService.ConfigureStartupWindow(selectionWindow, 350, 300);
            if (selectionWindow.ShowDialog() != true)
                return false;

            var selectedType = ((UserSelectionViewModel)selectionWindow.DataContext).SelectedUserType;

            var pinWindow = serviceProvider.GetRequiredService<PinLoginWindow>();
            sizingService.ConfigureStartupWindow(pinWindow, 320, 250);
            ((PinLoginViewModel)pinWindow.DataContext).UserType = selectedType;

            if (pinWindow.ShowDialog() == true)
            {
                userType = selectedType;
                return true;
            }
        }
    }
}
