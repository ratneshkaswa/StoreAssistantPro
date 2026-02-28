using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;

namespace StoreAssistantPro.Modules.SystemSettings.ViewModels;

public partial class AppInfoViewModel(
    ISessionService sessionService,
    IApplicationInfoService appInfo) : BaseViewModel
{
    public string AppVersion => appInfo.AppVersion;
    public string DotNetVersion => appInfo.DotNetVersion;
    public string Environment => appInfo.Environment;
    public string DatabaseServer => appInfo.DatabaseServer;
    public string DatabaseName => appInfo.DatabaseName;
    public string LogDirectory => appInfo.LogDirectory;
    public string CurrentUser => sessionService.CurrentUserType.ToString();
    public string FirmName => sessionService.FirmName;

    [ObservableProperty]
    public partial string DatabaseStatus { get; set; } = "Checking...";

    [ObservableProperty]
    public partial bool IsDatabaseConnected { get; set; }

    [ObservableProperty]
    public partial int PendingMigrationCount { get; set; }

    [RelayCommand]
    private async Task CheckConnectionAsync()
    {
        DatabaseStatus = "Checking...";
        IsDatabaseConnected = await appInfo.IsDatabaseConnectedAsync();
        DatabaseStatus = IsDatabaseConnected ? "Connected" : "Disconnected";

        var pending = await appInfo.GetPendingMigrationsAsync();
        PendingMigrationCount = pending.Count;
    }
}
