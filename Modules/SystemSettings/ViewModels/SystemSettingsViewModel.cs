using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core;

namespace StoreAssistantPro.Modules.SystemSettings.ViewModels;

public partial class SystemSettingsViewModel : BaseViewModel
{
    private readonly IServiceProvider _serviceProvider;

    public string[] Categories { get; } =
        ["General", "Security", "Backup & Restore", "Application Info"];

    [ObservableProperty]
    public partial string SelectedCategory { get; set; } = "General";

    [ObservableProperty]
    public partial ObservableObject? CurrentCategoryView { get; set; }

    public SystemSettingsViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        NavigateToCategory("General");
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        NavigateToCategory(value);
    }

    private void NavigateToCategory(string category)
    {
        CurrentCategoryView = category switch
        {
            "General" => _serviceProvider.GetRequiredService<GeneralSettingsViewModel>(),
            "Security" => _serviceProvider.GetRequiredService<SecuritySettingsViewModel>(),
            "Backup & Restore" => _serviceProvider.GetRequiredService<BackupSettingsViewModel>(),
            "Application Info" => _serviceProvider.GetRequiredService<AppInfoViewModel>(),
            _ => null
        };
    }
}
