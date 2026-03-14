using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.Products.ViewModels;

namespace StoreAssistantPro.Modules.Products.Views;

public partial class ProductManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 900;
    protected override double DialogHeight => 780;
    protected override double DialogMinWidth => 820;
    protected override double DialogMinHeight => 680;
    protected override bool AllowResize => true;

    private readonly IServiceProvider _sp;

    public ProductManagementWindow(
        IWindowSizingService sizingService,
        IServiceProvider sp,
        ProductManagementViewModel vm) : base(sizingService)
    {
        _sp = sp;
        InitializeComponent();
        DataContext = vm;
        vm.OpenVariantsDialog = product =>
        {
            var window = _sp.GetRequiredService<VariantManagementWindow>();
            window.SetProduct(product);
            window.Owner = this;
            window.ShowDialog();
        };
        Closed += (_, _) => vm.Dispose();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ProductManagementViewModel vm)
        {
            try { await vm.LoadCommand.ExecuteAsync(null); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
