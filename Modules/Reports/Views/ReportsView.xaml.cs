using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Reports.ViewModels;

namespace StoreAssistantPro.Modules.Reports.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ReportsViewModel vm)
            await vm.LoadReportsCommand.ExecuteAsync(null);
    }
}
