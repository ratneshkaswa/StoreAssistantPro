using System.Windows;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Startup.ViewModels;

namespace StoreAssistantPro.Modules.Startup.Views;

public partial class SetupWizardWindow : Window
{
    public SetupWizardWindow(
        IWindowSizingService sizingService,
        SetupWizardViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        sizingService.ConfigureStartupWindow(this, 640, 780);

        SourceInitialized += (_, _) => Win11Backdrop.Apply(this);

        vm.RequestClose = result =>
        {
            DialogResult = result;
            Close();
        };

        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SetupWizardViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
