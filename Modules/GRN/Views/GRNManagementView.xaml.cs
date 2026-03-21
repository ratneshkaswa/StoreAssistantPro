using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.GRN.ViewModels;

namespace StoreAssistantPro.Modules.GRN.Views;

public partial class GRNManagementView : UserControl
{
    public GRNManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is GRNViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
