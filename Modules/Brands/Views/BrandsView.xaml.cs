using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StoreAssistantPro.Modules.Brands.ViewModels;

namespace StoreAssistantPro.Modules.Brands.Views;

public partial class BrandsView : UserControl
{
    public BrandsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is BrandsViewModel vm)
        {
            await vm.LoadBrandsCommand.ExecuteAsync(null);
        }
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is BrandsViewModel vm
            && vm.SelectedBrand is not null
            && vm.ShowEditFormCommand.CanExecute(null))
        {
            vm.ShowEditFormCommand.Execute(null);
        }
    }
}
