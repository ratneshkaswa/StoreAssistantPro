using System.Windows;
using System.Windows.Controls;

namespace StoreAssistantPro.Modules.Products.Views;

public partial class VariantManagementView : UserControl
{
    public VariantManagementView()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Data loading is handled by INavigationAware.OnNavigatedTo in the ViewModel
    }
}
