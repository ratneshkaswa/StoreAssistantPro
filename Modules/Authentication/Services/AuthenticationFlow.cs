using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.Authentication.Views;

namespace StoreAssistantPro.Modules.Authentication.Services;

public class AuthenticationFlow(IServiceProvider serviceProvider) : IAuthenticationFlow
{
    public bool RunFirstTimeSetup()
    {
        var window = serviceProvider.GetRequiredService<FirstTimeSetupWindow>();
        return window.ShowDialog() == true;
    }

    public bool TryLogin(out UserType userType)
    {
        userType = default;

        while (true)
        {
            var selectionWindow = serviceProvider.GetRequiredService<UserSelectionWindow>();
            if (selectionWindow.ShowDialog() != true)
                return false;

            var selectedType = ((UserSelectionViewModel)selectionWindow.DataContext).SelectedUserType;

            var pinWindow = serviceProvider.GetRequiredService<PinLoginWindow>();
            ((PinLoginViewModel)pinWindow.DataContext).UserType = selectedType;

            if (pinWindow.ShowDialog() == true)
            {
                userType = selectedType;
                return true;
            }
        }
    }
}
