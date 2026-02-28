using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Inward.ViewModels;

namespace StoreAssistantPro.Modules.Inward.Views;

public partial class InwardView : UserControl
{
    public InwardView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InwardViewModel vm)
            await vm.LoadInwardCommand.ExecuteAsync(null);
    }
}
