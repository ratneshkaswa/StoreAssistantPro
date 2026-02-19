using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Modules.SystemSettings.Services;
using StoreAssistantPro.Core.Session;

namespace StoreAssistantPro.Modules.SystemSettings.ViewModels;

public partial class AppInfoViewModel(
    ISessionService sessionService,
    ISystemSettingsService settingsService) : BaseViewModel
{
    public string AppVersion { get; } =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    public string CurrentUser => sessionService.CurrentUserType.ToString();

    public string FirmName => sessionService.FirmName;

    [ObservableProperty]
    public partial string DatabaseStatus { get; set; } = "Checking...";

    [ObservableProperty]
    public partial bool IsDatabaseConnected { get; set; }

    [RelayCommand]
    private async Task CheckConnectionAsync()
    {
        DatabaseStatus = "Checking...";
        IsDatabaseConnected = await settingsService.CheckDatabaseConnectionAsync();
        DatabaseStatus = IsDatabaseConnected ? "Connected" : "Disconnected";
    }
}
