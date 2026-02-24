using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using StoreAssistantPro.Core;

namespace StoreAssistantPro.Modules.MainShell.ViewModels;

/// <summary>
/// ViewModel for the Tasks dialog. Displays pending tasks
/// and allows marking them as completed.
/// </summary>
public partial class TasksViewModel : BaseViewModel
{
    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public ObservableCollection<string> Tasks { get; } = [];

    public TasksViewModel()
    {
        IsEmpty = true;
    }
}
