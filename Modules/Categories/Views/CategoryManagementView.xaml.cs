using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Categories.ViewModels;

namespace StoreAssistantPro.Modules.Categories.Views;

public partial class CategoryManagementView : UserControl
{
    public CategoryManagementView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is CategoryManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
