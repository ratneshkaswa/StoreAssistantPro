using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Tax.ViewModels;

namespace StoreAssistantPro.Modules.Tax.Views;

public partial class TaxManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 560;
    protected override double DialogHeight => 520;

    public TaxManagementWindow(
        IWindowSizingService sizingService,
        TaxManagementViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is TaxManagementViewModel vm)
            await vm.LoadProfilesCommand.ExecuteAsync(null);
    }
}
