using System.Windows;
using System.Windows.Controls;
using StoreAssistantPro.Modules.Staff.ViewModels;

namespace StoreAssistantPro.Modules.Staff.Views;

public partial class StaffView : UserControl
{
    public StaffView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StaffViewModel vm)
            await vm.LoadStaffCommand.ExecuteAsync(null);
    }
}
