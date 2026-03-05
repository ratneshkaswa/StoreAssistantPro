using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Modules.Billing.ViewModels;

public partial class BillingViewModel(
    IBillingService billingService,
    IAppStateService appState,
    IDialogService dialogService) : BaseViewModel
{
    // ═══════════════════════════════════════════════════════════════
    //  Cart
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<CartLineViewModel> CartItems { get; set; } = [];

    [ObservableProperty]
    public partial CartLineViewModel? SelectedCartItem { get; set; }

    // ═══════════════════════════════════════════════════════════════
    //  Totals (recalculated on every cart change)
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial decimal Subtotal { get; set; }

    [ObservableProperty]
    public partial decimal DiscountAmount { get; set; }

    [ObservableProperty]
    public partial decimal GrandTotal { get; set; }

    // ═══════════════════════════════════════════════════════════════
    //  Discount
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial DiscountType SelectedDiscountType { get; set; } = DiscountType.None;

    [ObservableProperty]
    public partial string DiscountInput { get; set; } = "0";

    [ObservableProperty]
    public partial string DiscountReason { get; set; } = string.Empty;

    public ObservableCollection<DiscountType> DiscountTypes { get; } =
    [
        DiscountType.None,
        DiscountType.Amount,
        DiscountType.Percentage
    ];

    partial void OnSelectedDiscountTypeChanged(DiscountType value) => RecalculateTotals();
    partial void OnDiscountInputChanged(string value) => RecalculateTotals();

    // ═══════════════════════════════════════════════════════════════
    //  Payment
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial string PaymentMethod { get; set; } = "Cash";

    [ObservableProperty]
    public partial string PaymentReference { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CashTendered { get; set; } = "0";

    [ObservableProperty]
    public partial decimal ChangeAmount { get; set; }

    public ObservableCollection<string> PaymentMethods { get; } =
        ["Cash", "UPI", "Card"];

    partial void OnCashTenderedChanged(string value) => RecalculateChange();

    // ═══════════════════════════════════════════════════════════════
    //  Search / Barcode
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<Product> SearchResults { get; set; } = [];

    [ObservableProperty]
    public partial string BarcodeInput { get; set; } = string.Empty;

    [RelayCommand]
    private Task SearchProductsAsync() => RunAsync(async ct =>
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults = [];
            return;
        }
        var results = await billingService.SearchProductsAsync(SearchText, ct);
        SearchResults = new ObservableCollection<Product>(results);
    });

    [RelayCommand]
    private Task ScanBarcodeAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var result = await billingService.LookupByBarcodeAsync(BarcodeInput, ct);
        if (result is null)
        {
            ErrorMessage = $"Barcode '{BarcodeInput}' not found.";
            BarcodeInput = string.Empty;
            return;
        }

        AddToCart(result.Product, result.Variant);
        BarcodeInput = string.Empty;
    });

    [RelayCommand]
    private void AddProductToCart(Product? product)
    {
        if (product is null) return;
        AddToCart(product, null);
        SearchText = string.Empty;
        SearchResults = [];
    }

    // ═══════════════════════════════════════════════════════════════
    //  Cart Operations
    // ═══════════════════════════════════════════════════════════════

    private void AddToCart(Product product, ProductVariant? variant)
    {
        var existing = CartItems.FirstOrDefault(c =>
            c.ProductId == product.Id &&
            c.ProductVariantId == variant?.Id);

        if (existing is not null)
        {
            existing.Quantity++;
        }
        else
        {
            var price = product.SalePrice;
            if (variant is not null)
                price += variant.AdditionalPrice;

            var line = new CartLineViewModel
            {
                ProductId = product.Id,
                ProductVariantId = variant?.Id,
                ProductName = variant is not null
                    ? $"{product.Name} ({variant.Size?.Name}/{variant.Colour?.Name})"
                    : product.Name,
                UnitPrice = price,
                Quantity = 1
            };
            line.PropertyChanged += (_, _) => RecalculateTotals();
            CartItems.Add(line);
        }

        RecalculateTotals();
        ClearMessages();
    }

    [RelayCommand]
    private void RemoveCartItem()
    {
        if (SelectedCartItem is null) return;
        CartItems.Remove(SelectedCartItem);
        SelectedCartItem = null;
        RecalculateTotals();
    }

    [RelayCommand]
    private void ClearCart()
    {
        if (CartItems.Count == 0) return;
        if (!dialogService.Confirm("Clear the entire cart?", "Cancel Bill"))
            return;

        CartItems.Clear();
        SelectedCartItem = null;
        ResetPayment();
        RecalculateTotals();
        ClearMessages();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Complete Sale
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private Task CompleteSaleAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!Validate(v => v
            .Rule(CartItems.Count > 0, "Cart is empty.")
            .Rule(!string.IsNullOrWhiteSpace(PaymentMethod), "Select a payment method.")))
            return;

        if (PaymentMethod == "Cash" && decimal.TryParse(CashTendered, out var tendered) && tendered < GrandTotal)
        {
            ErrorMessage = "Cash tendered is less than the total amount.";
            return;
        }

        var items = CartItems.Select(c => new CartItemDto(
            c.ProductId,
            c.ProductVariantId,
            c.Quantity,
            c.UnitPrice,
            c.ItemDiscountRate)).ToList();

        _ = decimal.TryParse(DiscountInput, out var discVal);

        var dto = new CompleteSaleDto(
            items,
            PaymentMethod,
            string.IsNullOrWhiteSpace(PaymentReference) ? null : PaymentReference.Trim(),
            SelectedDiscountType,
            discVal,
            string.IsNullOrWhiteSpace(DiscountReason) ? null : DiscountReason.Trim(),
            decimal.TryParse(CashTendered, out var cash) ? cash : 0,
            appState.CurrentUserType.ToString(),
            Guid.NewGuid());

        var sale = await billingService.CompleteSaleAsync(dto, ct);
        SuccessMessage = $"Sale {sale.InvoiceNumber} completed — ₹{sale.TotalAmount:N2}";

        CartItems.Clear();
        SelectedCartItem = null;
        ResetPayment();
        RecalculateTotals();
    });

    // ═══════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════

    private void RecalculateTotals()
    {
        Subtotal = CartItems.Sum(c => c.LineTotal);

        _ = decimal.TryParse(DiscountInput, out var discVal);
        DiscountAmount = SelectedDiscountType switch
        {
            DiscountType.Amount => Math.Min(discVal, Subtotal),
            DiscountType.Percentage => Math.Min(Subtotal * discVal / 100m, Subtotal),
            _ => 0
        };

        GrandTotal = Math.Max(0, Subtotal - DiscountAmount);
        RecalculateChange();
    }

    private void RecalculateChange()
    {
        if (decimal.TryParse(CashTendered, out var tendered) && tendered > GrandTotal)
            ChangeAmount = tendered - GrandTotal;
        else
            ChangeAmount = 0;
    }

    private void ResetPayment()
    {
        SelectedDiscountType = DiscountType.None;
        DiscountInput = "0";
        DiscountReason = string.Empty;
        PaymentMethod = "Cash";
        PaymentReference = string.Empty;
        CashTendered = "0";
        ChangeAmount = 0;
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}

/// <summary>
/// Single line item in the billing cart.
/// </summary>
public partial class CartLineViewModel : ObservableObject
{
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }

    [ObservableProperty]
    public partial string ProductName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    public partial decimal UnitPrice { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    public partial int Quantity { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    public partial decimal ItemDiscountRate { get; set; }

    public decimal LineTotal => Quantity * UnitPrice * (1 - ItemDiscountRate / 100m);
}
