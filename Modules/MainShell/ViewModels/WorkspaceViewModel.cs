using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

public partial class WorkspaceViewModel(IDashboardService dashboardService) : BaseViewModel
{
    [RelayCommand]
    private Task LoadMainWorkspaceAsync() => RunLoadAsync(async ct =>
    {
        await dashboardService.GetSummaryAsync(ct);
    });
}
