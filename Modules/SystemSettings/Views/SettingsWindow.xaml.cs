using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.SystemSettings.ViewModels;

namespace StoreAssistantPro.Modules.SystemSettings.Views;

public partial class SettingsWindow : BaseDialogWindow
{
    protected override double DialogWidth => 780;
    protected override double DialogHeight => 520;

    public SettingsWindow(
        IWindowSizingService sizingService,
        SettingsViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
