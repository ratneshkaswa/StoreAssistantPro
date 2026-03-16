using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Expenses.ViewModels;

namespace StoreAssistantPro.Modules.Expenses.Views;

public partial class ExpenseManagementView : UserControl
{
    public ExpenseManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ExpenseManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
