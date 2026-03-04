using System.Windows;
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
