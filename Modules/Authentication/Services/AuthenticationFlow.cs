using System.Windows;
using System.Windows.Media.Imaging;
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
        sizingService.ConfigureStartupWindow(window, 480, 840);
        TrySetAppIcon(window);
        return window.ShowDialog() == true;
    }

    public bool TryLogin(out UserType userType)
    {
        userType = default;

        var loginWindow = serviceProvider.GetRequiredService<UnifiedLoginWindow>();
        sizingService.ConfigureStartupWindow(loginWindow, 420, 640);
        TrySetAppIcon(loginWindow);

        if (loginWindow.ShowDialog() != true)
            return false;

        var vm = (UnifiedLoginViewModel)loginWindow.DataContext;
        userType = vm.SelectedUserType!.Value;
        return true;
    }

    private static void TrySetAppIcon(Window window)
    {
        var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
        var info = Application.GetResourceStream(uri);
        if (info != null)
        {
            window.Icon = BitmapFrame.Create(info.Stream);
        }
    }
}
