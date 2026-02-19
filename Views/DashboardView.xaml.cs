using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.ViewModels;

namespace StoreAssistantPro.Views;

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
