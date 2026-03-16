using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Salaries.ViewModels;

namespace StoreAssistantPro.Modules.Salaries.Views;

public partial class SalaryManagementView : UserControl
{
    public SalaryManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SalaryManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
