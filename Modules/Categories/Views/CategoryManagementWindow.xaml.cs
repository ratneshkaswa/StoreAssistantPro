using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Categories.ViewModels;

namespace StoreAssistantPro.Modules.Categories.Views;

public partial class CategoryManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 900;
    protected override double DialogHeight => 720;
    protected override double DialogMinWidth => 780;
    protected override double DialogMinHeight => 620;
    protected override bool AllowResize => true;

    public CategoryManagementWindow(
        IWindowSizingService sizingService,
        CategoryManagementViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        RunDeferredInitialLoad(async () =>
        {
            if (DataContext is CategoryManagementViewModel vm)
            {
                try { await vm.LoadCommand.ExecuteAsync(null); }
                catch { /* RunLoadAsync handles logging */ }
            }
        });
}
