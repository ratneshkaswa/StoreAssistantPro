using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Sales.Commands;
using StoreAssistantPro.Modules.Sales.Services;
using StoreAssistantPro.Core.Session;

namespace StoreAssistantPro.Modules.Sales.ViewModels;

public partial class SalesViewModel(
    ISalesService salesService,
    IProductService productService,
    ISessionService sessionService,
    ICommandBus commandBus) : BaseViewModel
{
    // ── Role-based access ──

    public bool CanCreateSales =>
        sessionService.CurrentUserType is UserType.Admin or UserType.Manager;
    [ObservableProperty]
    public partial ObservableCollection<Sale> Sales { get; set; } = [];

    [ObservableProperty]
    public partial Sale? SelectedSale { get; set; }

    public bool HasSelectedSale => SelectedSale is not null;

    partial void OnSelectedSaleChanged(Sale? value) =>
        OnPropertyChanged(nameof(HasSelectedSale));

    [ObservableProperty]
    public partial DateTime FilterFrom { get; set; } = DateTime.Today;

    [ObservableProperty]
    public partial DateTime FilterTo { get; set; } = DateTime.Today;

    // New Sale form
    [ObservableProperty]
    public partial bool IsNewSaleVisible { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<Product> AvailableProducts { get; set; } = [];

    [ObservableProperty]
    public partial Product? SelectedProduct { get; set; }

    [ObservableProperty]
    public partial int CartQuantity { get; set; } = 1;

    [ObservableProperty]
    public partial string PaymentMethod { get; set; } = "Cash";

    public string[] PaymentMethods { get; } = ["Cash", "Card", "Transfer"];

    [ObservableProperty]
    public partial ObservableCollection<SaleItem> CartItems { get; set; } = [];

    [ObservableProperty]
    public partial decimal CartTotal { get; set; }

    [RelayCommand]
    private async Task LoadSalesAsync()
    {
        IsLoading = true;
        try
        {
            var items = await salesService.GetAllAsync();
            Sales = new ObservableCollection<Sale>(items);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FilterByDateAsync()
    {
        var items = await salesService.GetSalesByDateRangeAsync(FilterFrom.Date, FilterTo.Date.AddDays(1));
        Sales = new ObservableCollection<Sale>(items);
    }

    [RelayCommand]
    private async Task ShowNewSaleAsync()
    {
        if (!CanCreateSales)
        {
            ErrorMessage = "Only administrators and managers can create sales.";
            return;
        }

        ErrorMessage = string.Empty;
        var products = await productService.GetAllAsync();
        AvailableProducts = new ObservableCollection<Product>(products.Where(p => p.Quantity > 0));
        CartItems = [];
        CartTotal = 0;
        CartQuantity = 1;
        PaymentMethod = "Cash";
        SelectedProduct = null;
        IsNewSaleVisible = true;
    }

    [RelayCommand]
    private void CancelNewSale()
    {
        IsNewSaleVisible = false;
    }

    [RelayCommand]
    private void AddToCart()
    {
        ErrorMessage = string.Empty;

        if (SelectedProduct is null || CartQuantity <= 0) return;

        var existing = CartItems.FirstOrDefault(i => i.ProductId == SelectedProduct.Id);
        var totalRequested = CartQuantity + (existing?.Quantity ?? 0);

        if (totalRequested > SelectedProduct.Quantity)
        {
            ErrorMessage = $"Only {SelectedProduct.Quantity} available for '{SelectedProduct.Name}'. Already {existing?.Quantity ?? 0} in cart.";
            return;
        }

        if (existing is not null)
        {
            existing.Quantity += CartQuantity;
            existing.UnitPrice = SelectedProduct.Price;
            CartItems = new ObservableCollection<SaleItem>(CartItems);
        }
        else
        {
            CartItems.Add(new SaleItem
            {
                ProductId = SelectedProduct.Id,
                Product = SelectedProduct,
                Quantity = CartQuantity,
                UnitPrice = SelectedProduct.Price
            });
        }

        CartTotal = CartItems.Sum(i => i.Subtotal);
        CartQuantity = 1;
        SelectedProduct = null;
    }

    [RelayCommand]
    private void RemoveFromCart(SaleItem? item)
    {
        if (item is null) return;

        CartItems.Remove(item);
        CartTotal = CartItems.Sum(i => i.Subtotal);
    }

    [RelayCommand]
    private async Task CompleteSaleAsync()
    {
        ErrorMessage = string.Empty;

        if (CartItems.Count == 0) return;

        var items = CartItems.Select(i =>
            new SaleItemDto(i.ProductId, i.Quantity, i.UnitPrice)).ToList();

        var result = await commandBus.SendAsync(
            new CompleteSaleCommand(CartTotal, PaymentMethod, items));

        if (result.Succeeded)
        {
            IsNewSaleVisible = false;
            await LoadSalesAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage ?? "Sale failed.";
        }
    }
}
