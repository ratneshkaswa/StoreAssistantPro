using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Brands.ViewModels;

namespace StoreAssistantPro.Modules.Brands.Views;

public partial class BrandManagementView : UserControl
{
    public BrandManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BrandManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
