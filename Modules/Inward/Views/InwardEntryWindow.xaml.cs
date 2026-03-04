using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Inward.ViewModels;

namespace StoreAssistantPro.Modules.Inward.Views;

public partial class InwardEntryWindow : BaseDialogWindow
{
    protected override double DialogWidth => 900;
    protected override double DialogHeight => 680;

    public InwardEntryWindow(
        IWindowSizingService sizingService,
        InwardEntryViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InwardEntryViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
