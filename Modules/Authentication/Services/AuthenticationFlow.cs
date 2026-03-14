using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.ViewModels;
using StoreAssistantPro.Modules.Authentication.Views;

namespace StoreAssistantPro.Modules.Authentication.Services;

public class AuthenticationFlow(
    IServiceProvider serviceProvider,
    IWindowSizingService sizingService,
    ILogger<AuthenticationFlow> logger) : IAuthenticationFlow
{
    public bool RunFirstTimeSetup()
    {
        try
        {
            var window = serviceProvider.GetRequiredService<SetupWindow>();
            var dialogResult = window.ShowDialog();
            if (dialogResult == true)
                return true;

            return (window.DataContext as SetupViewModel)?.IsSetupComplete == true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open setup window");
            AppDialogPresenter.ShowError(
                "Unable to Open Window",
                "Setup could not be opened.\n\nThe error has been logged.");
            return false;
        }
    }

    public bool TryLogin(out UserType userType)
    {
        userType = default;

        try
        {
            var loginWindow = serviceProvider.GetRequiredService<LoginWindow>();
            sizingService.ConfigureStartupWindow(loginWindow, 560, 760);

            if (loginWindow.ShowDialog() != true)
                return false;

            var vm = (LoginViewModel)loginWindow.DataContext;
            userType = vm.SelectedUserType!.Value;
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to open login window");
            AppDialogPresenter.ShowError(
                "Unable to Open Window",
                "Login could not be opened.\n\nThe error has been logged.");
            return false;
        }
    }
}
