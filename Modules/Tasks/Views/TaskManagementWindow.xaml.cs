using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Tasks.ViewModels;

namespace StoreAssistantPro.Modules.Tasks.Views;

public partial class TaskManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 960;
    protected override double DialogHeight => 750;
    protected override double DialogMinWidth => 900;
    protected override double DialogMinHeight => 700;
    protected override bool AllowResize => true;

    public TaskManagementWindow(
        IWindowSizingService sizingService,
        TaskManagementViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        RunDeferredInitialLoad(async () =>
        {
            if (DataContext is TaskManagementViewModel vm)
            {
                try { await vm.LoadCommand.ExecuteAsync(null); }
                catch { /* RunLoadAsync handles logging */ }
            }
        });
}
