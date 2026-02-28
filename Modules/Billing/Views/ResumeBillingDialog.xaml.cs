using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Billing.ViewModels;

namespace StoreAssistantPro.Modules.Billing.Views;

public partial class ResumeBillingDialog : BaseDialogWindow
{
    protected override double DialogWidth => 440;
    protected override double DialogHeight => 360;

    public ResumeBillingDialog(
        IWindowSizingService sizingService,
        ResumeBillingDialogViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;

        vm.CloseDialog = result =>
        {
            try { DialogResult = result; }
            catch (InvalidOperationException) { Close(); }
        };
    }
}
