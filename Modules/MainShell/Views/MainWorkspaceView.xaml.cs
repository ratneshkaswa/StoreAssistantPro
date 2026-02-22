using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class MainWorkspaceView : UserControl
{
    public MainWorkspaceView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWorkspaceViewModel vm)
        {
            await vm.LoadMainWorkspaceCommand.ExecuteAsync(null);
        }
    }
}
