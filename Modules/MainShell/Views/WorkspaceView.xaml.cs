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
            try
            {
                await vm.LoadMainWorkspaceCommand.ExecuteAsync(null);
            }
            catch (Exception)
            {
                // RunLoadAsync inside the VM already captures and logs
                // exceptions. This guard is defensive against edge cases
                // where the command infrastructure itself throws.
            }
        }
    }
}
