using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Branch.ViewModels;

namespace StoreAssistantPro.Modules.Branch.Views;

public partial class BranchManagementView : UserControl
{
    public BranchManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BranchManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
