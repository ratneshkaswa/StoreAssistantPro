using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Settings.ViewModels;

namespace StoreAssistantPro.Modules.Settings.Views;

public partial class SystemSettingsWindow : BaseDialogWindow
{
    protected override double DialogWidth => 550;
    protected override double DialogHeight => 580;

    public SystemSettingsWindow(
        IWindowSizingService sizingService,
        SystemSettingsViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SystemSettingsViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
