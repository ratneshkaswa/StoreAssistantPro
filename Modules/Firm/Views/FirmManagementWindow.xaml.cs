using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Firm.ViewModels;

namespace StoreAssistantPro.Modules.Firm.Views;

public partial class FirmManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 450;
    protected override double DialogHeight => 350;

    public FirmManagementWindow(
        IWindowSizingService sizingService,
        FirmManagementViewModel vm) : base(sizingService)
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
