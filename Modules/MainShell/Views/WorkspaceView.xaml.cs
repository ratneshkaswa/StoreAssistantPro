using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }

        if (StartBillingButton.Visibility == Visibility.Visible)
        {
            _ = Dispatcher.BeginInvoke(
                () => StartBillingButton.Focus(),
                DispatcherPriority.Input);
        }
    }
}
