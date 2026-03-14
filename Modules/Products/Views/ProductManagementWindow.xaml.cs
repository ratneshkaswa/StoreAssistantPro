using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ProductManagementWindow> _logger;

    public ProductManagementWindow(
        IWindowSizingService sizingService,
        IServiceProvider sp,
        ILogger<ProductManagementWindow> logger,
        ProductManagementViewModel vm) : base(sizingService)
    {
        _sp = sp;
        _logger = logger;
        InitializeComponent();
        DataContext = vm;
        vm.OpenVariantsDialog = product =>
        {
            try
            {
                var window = _sp.GetRequiredService<VariantManagementWindow>();
                window.SetProduct(product);
                window.Owner = this;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open VariantManagementWindow for product {ProductId}", product.Id);
                AppDialogPresenter.ShowError(
                    "Unable to Open Window",
                    $"Variants could not be opened for {product.Name}.\n\nThe error has been logged.",
                    this);
            }
        };
        Closed += (_, _) => vm.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        RunDeferredInitialLoad(async () =>
        {
            if (DataContext is ProductManagementViewModel vm)
            {
                try { await vm.LoadCommand.ExecuteAsync(null); }
                catch { /* RunLoadAsync handles logging */ }
            }
        });
}
