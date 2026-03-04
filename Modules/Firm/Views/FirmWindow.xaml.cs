using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Firm.ViewModels;

namespace StoreAssistantPro.Modules.Firm.Views;

public partial class FirmWindow : BaseDialogWindow
{
    protected override double DialogWidth => 480;
    protected override double DialogHeight => 590;

    public FirmWindow(
        IWindowSizingService sizingService,
        FirmViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is FirmViewModel vm)
        {
            try
            {
                await vm.LoadFirmCommand.ExecuteAsync(null);
            }
            catch (Exception)
            {
                // RunLoadAsync inside the VM already captures and logs
                // exceptions. This guard is defensive against edge cases
                // where the command infrastructure itself throws.
            }
        }
    }
}
