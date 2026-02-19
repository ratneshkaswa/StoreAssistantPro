using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.SystemSettings.ViewModels;

namespace StoreAssistantPro.Modules.SystemSettings.Views;

public partial class AppInfoView : UserControl
{
    public AppInfoView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppInfoViewModel vm)
            await vm.CheckConnectionCommand.ExecuteAsync(null);
    }
}
