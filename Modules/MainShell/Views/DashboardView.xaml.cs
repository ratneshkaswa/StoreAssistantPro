using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            await vm.LoadDashboardCommand.ExecuteAsync(null);
        }
    }
}
