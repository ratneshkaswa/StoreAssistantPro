using System.Windows;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.ViewModels;

namespace StoreAssistantPro.Modules.Products.Views;

public partial class VariantManagementWindow : BaseDialogWindow
{
    protected override double DialogWidth => 1100;
    protected override double DialogHeight => 800;

    private readonly VariantManagementViewModel _vm;

    public VariantManagementWindow(
        IWindowSizingService sizingService,
        VariantManagementViewModel vm) : base(sizingService)
    {
        _vm = vm;
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }

    /// <summary>
    /// Sets the product context before loading data.
    /// Must be called before the window is shown.
    /// </summary>
    public void SetProduct(Product product) => _vm.Product = product;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_vm.Product is not null)
        {
            try { await _vm.InitializeAsync(_vm.Product); }
            catch { /* RunLoadAsync handles logging */ }
        }
    }
}
