using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.SystemSettings.ViewModels;

namespace StoreAssistantPro.Modules.SystemSettings.Views;

public partial class SystemSettingsWindow : BaseDialogWindow
{
    protected override double DialogWidth => 780;
    protected override double DialogHeight => 520;

    public SystemSettingsWindow(
        IWindowSizingService sizingService,
        SystemSettingsViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
