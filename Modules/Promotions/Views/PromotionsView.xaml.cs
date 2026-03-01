using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Promotions.ViewModels;

namespace StoreAssistantPro.Modules.Promotions.Views;

public partial class PromotionsView : UserControl
{
    public PromotionsView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is PromotionsViewModel vm)
            await vm.LoadPromotionsCommand.ExecuteAsync(null);
    }
}
