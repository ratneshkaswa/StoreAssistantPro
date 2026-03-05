using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;

namespace StoreAssistantPro.Modules.Billing.Views;

public partial class BillingWindow : BaseDialogWindow
{
    protected override double DialogWidth => 1200;
    protected override double DialogHeight => 850;

    public BillingWindow(
        IWindowSizingService sizingService,
        BillingViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Billing window starts with empty cart, no async load needed
    }
}
