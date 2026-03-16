using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Debtors.ViewModels;

namespace StoreAssistantPro.Modules.Debtors.Views;

public partial class DebtorManagementView : UserControl
{
    public DebtorManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DebtorManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
