using System;
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

        if (StartBillingFab.Visibility == Visibility.Visible)
        {
            _ = Dispatcher.BeginInvoke(
                () => StartBillingFab.Focus(),
                DispatcherPriority.Input);
        }
    }

    private void OnWorkspaceScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        HeroBackdropParallaxTransform.Y = Math.Min(e.VerticalOffset * 0.5, 56);
    }
}
