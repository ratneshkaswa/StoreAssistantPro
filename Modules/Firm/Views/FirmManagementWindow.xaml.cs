using System.Windows;
using StoreAssistantPro.Modules.Firm.ViewModels;

namespace StoreAssistantPro.Modules.Firm.Views;

public partial class FirmManagementWindow : Window
{
    public FirmManagementWindow(FirmManagementViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is FirmManagementViewModel vm)
            await vm.LoadFirmCommand.ExecuteAsync(null);
    }
}
