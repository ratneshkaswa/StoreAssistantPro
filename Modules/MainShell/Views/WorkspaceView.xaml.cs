using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class WorkspaceView : UserControl
{
    public WorkspaceView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (StartBillingButton.Visibility != Visibility.Visible)
            return;

        Dispatcher.BeginInvoke(
            () => StartBillingButton.Focus(),
            DispatcherPriority.Input);
    }
}
