using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Settings.ViewModels;

namespace StoreAssistantPro.Modules.Settings.Views;

public partial class SystemSettingsWindow : BaseDialogWindow
{
    protected override double DialogWidth => 860;
    protected override double DialogHeight => 720;
    protected override double DialogMinWidth => 700;
    protected override double DialogMinHeight => 600;
    protected override bool AllowResize => true;

    public SystemSettingsWindow(
        IWindowSizingService sizingService,
        SystemSettingsViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        RunDeferredInitialLoad(async () =>
        {
            if (DataContext is SystemSettingsViewModel vm)
            {
                try { await vm.LoadCommand.ExecuteAsync(null); }
                catch { /* RunLoadAsync handles logging */ }
            }
        });
}
