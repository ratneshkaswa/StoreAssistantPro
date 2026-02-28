using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Modules.MainShell.Views;

public partial class TasksWindow : BaseDialogWindow
{
    protected override double DialogWidth => 420;
    protected override double DialogHeight => 400;

    public TasksWindow(
        IWindowSizingService sizingService,
        TasksViewModel vm) : base(sizingService)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
