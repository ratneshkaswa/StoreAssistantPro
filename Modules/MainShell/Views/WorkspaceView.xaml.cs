using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class WorkspaceView : UserControl
{
    public WorkspaceView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is WorkspaceViewModel vm)
        {
            await vm.LoadMainWorkspaceCommand.ExecuteAsync(null);
        }
    }
}
