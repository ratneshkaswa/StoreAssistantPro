using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Tax.ViewModels;

namespace StoreAssistantPro.Modules.Tax.Views;

public partial class TaxManagementView : UserControl
{
    public TaxManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TaxManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
