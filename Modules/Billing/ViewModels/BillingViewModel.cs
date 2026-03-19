using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Services;
using StoreAssistantPro.Modules.Customers.Services;

namespace StoreAssistantPro.Modules.Billing.ViewModels;

public partial class BillingViewModel : BaseViewModel
{
    private readonly IBillingService _billingService;
    private readonly ICustomerService _customerService;
    private readonly IAppStateService _appState;
    private readonly IDialogService _dialogService;
    private readonly IRegionalSettingsService _regional;
    private readonly HashSet<CartLineViewModel> _trackedCartLines = [];
    private ObservableCollection<CartLineViewModel>? _trackedCartItems;
    private BillingSessionState? _sessionState;

    public BillingViewModel(
        IBillingService billingService,
        ICustomerService customerService,
        IAppStateService appState,
        IDialogService dialogService,
        IRegionalSettingsService regional)
    {
        _billingService = billingService;
        _customerService = customerService;
        _appState = appState;
        _dialogService = dialogService;
        _regional = regional;

        AttachCartCollection(CartItems);
        _trackedCartItems = CartItems;
        TransitionBillingSession(BillingSessionState.None);
    }

    // ═══════════════════════════════════════════════════════════════
    //  Cart
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial ObservableCollection<CartLineViewModel> CartItems { get; set; } = [];

    [ObservableProperty]
    public partial CartLineViewModel? SelectedCartItem { get; set; }

    partial void OnCartItemsChanged(ObservableCollection<CartLineViewModel> value)
    {
        if (ReferenceEquals(_trackedCartItems, value))
        {
            return;
        }

        if (_trackedCartItems is not null)
        {
            DetachCartCollection(_trackedCartItems);
        }

        AttachCartCollection(value);
        _trackedCartItems = value;

        if (SelectedCartItem is not null && !value.Contains(SelectedCartItem))
        {
            SelectedCartItem = null;
        }

        UpdateCartCommandStates();
        RecalculateTotals();
        HandleCartEmptiedState();
    }

    partial void OnSelectedCartItemChanged(CartLineViewModel? value) => UpdateCartCommandStates();

    // ═══════════════════════════════════════════════════════════════
    //  Totals (recalculated on every cart change)
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial decimal Subtotal { get; set; }

    [ObservableProperty]
    public partial decimal TotalTax { get; set; }

    [ObservableProperty]
    public partial decimal TotalCgst { get; set; }

    [ObservableProperty]
    public partial decimal TotalSgst { get; set; }

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
    [NotifyPropertyChangedFor(nameof(IsCashPayment))]
    [NotifyPropertyChangedFor(nameof(RequiresPaymentReference))]
    public partial string PaymentMethod { get; set; } = "Cash";

    [ObservableProperty]
    public partial string PaymentReference { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CashTendered { get; set; } = "0";

    [ObservableProperty]
    public partial decimal ChangeAmount { get; set; }

    public bool IsCashPayment => string.Equals(PaymentMethod, "Cash", StringComparison.OrdinalIgnoreCase);
    public bool RequiresPaymentReference => !IsCashPayment;

    public ObservableCollection<string> PaymentMethods { get; } =
        ["Cash", "UPI", "Card"];

    partial void OnPaymentMethodChanged(string value)
    {
        if (IsCashPayment)
        {
            PaymentReference = string.Empty;
        }
        else
        {
            CashTendered = "0";
        }

        RecalculateChange();
    }

    partial void OnCashTenderedChanged(string value) => RecalculateChange();

    // ═══════════════════════════════════════════════════════════════
    //  Customer Selection (#157 / #158)
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial string CustomerSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomerSearchResults))]
    public partial ObservableCollection<Customer> CustomerSearchResults { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedCustomer))]
    [NotifyPropertyChangedFor(nameof(CustomerDisplayName))]
    public partial Customer? SelectedCustomer { get; set; }

    public bool HasCustomerSearchResults => CustomerSearchResults.Count > 0;
    public bool HasSelectedCustomer => SelectedCustomer is not null;
    public string CustomerDisplayName => SelectedCustomer?.Name ?? "Walk-in";

    [RelayCommand]
    private Task SearchCustomersAsync() => RunAsync(async ct =>
    {
        if (string.IsNullOrWhiteSpace(CustomerSearchText))
        {
            CustomerSearchResults = [];
            return;
        }

        var results = await _customerService.SearchAsync(CustomerSearchText.Trim(), ct);
        CustomerSearchResults = new ObservableCollection<Customer>(results);
    });

    [RelayCommand]
    private void SelectCustomer(Customer? customer)
    {
        SelectedCustomer = customer;
        CustomerSearchText = string.Empty;
        CustomerSearchResults = [];
    }

    [RelayCommand]
    private void ClearCustomer()
    {
        SelectedCustomer = null;
        CustomerSearchText = string.Empty;
        CustomerSearchResults = [];
    }

    // ═══════════════════════════════════════════════════════════════
    //  Search / Barcode
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    public partial ObservableCollection<Product> SearchResults { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedSearchResult))]
    public partial Product? SelectedSearchResult { get; set; }

    [ObservableProperty]
    public partial string BarcodeInput { get; set; } = string.Empty;

    public bool HasSearchResults => SearchResults.Count > 0;
    public bool HasSelectedSearchResult => SelectedSearchResult is not null;

    partial void OnSelectedSearchResultChanged(Product? value) => AddProductToCartCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private Task SearchProductsAsync() => RunAsync(async ct =>
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SelectedSearchResult = null;
            SearchResults = [];
            return;
        }

        SelectedSearchResult = null;
        var results = await _billingService.SearchProductsAsync(SearchText.Trim(), ct);
        SearchResults = new ObservableCollection<Product>(results);
    });

    [RelayCommand]
    private Task ScanBarcodeAsync() => RunAsync(async ct =>
    {
        ClearMessages();
        if (string.IsNullOrWhiteSpace(BarcodeInput)) return;

        var result = await _billingService.LookupByBarcodeAsync(BarcodeInput, ct);
        if (result is null)
        {
            ErrorMessage = $"Barcode '{BarcodeInput}' not found.";
            BarcodeInput = string.Empty;
            return;
        }

        AddToCart(result.Product, result.Variant);
        BarcodeInput = string.Empty;
    });

    [RelayCommand(CanExecute = nameof(CanAddProductToCart))]
    private void AddProductToCart(Product? product)
    {
        if (product is null) return;
        AddToCart(product, null);
        SearchText = string.Empty;
        SearchResults = [];
        SelectedSearchResult = null;
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
                Quantity = 1,
                TaxRate = product.Tax?.SlabPercent ?? 0,
                IsTaxInclusive = product.IsTaxInclusive
            };
            CartItems.Add(line);
        }

        TransitionBillingSession(BillingSessionState.Active);
        UpdateCartCommandStates();
        RecalculateTotals();
        ClearMessages();
    }

    [RelayCommand(CanExecute = nameof(CanRemoveCartItem))]
    private void RemoveCartItem()
    {
        if (SelectedCartItem is null) return;
        CartItems.Remove(SelectedCartItem);
        SelectedCartItem = null;
        UpdateCartCommandStates();
        RecalculateTotals();
    }

    [RelayCommand(CanExecute = nameof(CanClearCart))]
    private void ClearCart()
    {
        if (CartItems.Count == 0) return;
        if (!_dialogService.Confirm("Clear the entire cart?", "Cancel Bill"))
            return;

        CartItems.Clear();
        TransitionBillingSession(BillingSessionState.Cancelled);
        SelectedCartItem = null;
        ResetPayment();
        UpdateCartCommandStates();
        RecalculateTotals();
        ClearMessages();
    }

    // ═══════════════════════════════════════════════════════════════
    //  Complete Sale
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand(CanExecute = nameof(CanCompleteSale))]
    private Task CompleteSaleAsync() => RunAsync(async ct =>
    {
        ClearMessages();

        if (!TryParseDiscountValue(out var discountValue, out var discountError))
        {
            ErrorMessage = discountError!;
            return;
        }

        if (!Validate(v => v
            .Rule(CartItems.Count > 0, "Cart is empty.")
            .Rule(!string.IsNullOrWhiteSpace(PaymentMethod), "Select a payment method.")
            .Rule(IsValidCart(), "Fix cart lines with invalid quantity, price, or discount.")
            .Rule(!RequiresPaymentReference || !string.IsNullOrWhiteSpace(PaymentReference),
                  "Enter a payment reference for non-cash payments.")))
            return;

        var cashTendered = 0m;
        if (IsCashPayment)
        {
            if (!decimal.TryParse(CashTendered, out cashTendered))
            {
                ErrorMessage = "Enter a valid cash amount.";
                return;
            }

            if (cashTendered < GrandTotal)
            {
                ErrorMessage = "Cash tendered is less than the total amount.";
                return;
            }
        }

        var items = CartItems.Select(c => new CartItemDto(
            c.ProductId,
            c.ProductVariantId,
            c.Quantity,
            c.UnitPrice,
            c.ItemDiscountRate,
            c.ItemDiscountAmount,
            c.TaxRate,
            c.IsTaxInclusive,
            c.LineTaxAmount)).ToList();

        var dto = new CompleteSaleDto(
            items,
            PaymentMethod,
            string.IsNullOrWhiteSpace(PaymentReference) ? null : PaymentReference.Trim(),
            SelectedDiscountType,
            discountValue,
            string.IsNullOrWhiteSpace(DiscountReason) ? null : DiscountReason.Trim(),
            cashTendered,
            _appState.CurrentUserType.ToString(),
            Guid.NewGuid(),
            SelectedCustomer?.Id);

        var sale = await _billingService.CompleteSaleAsync(dto, ct);
        SuccessMessage = $"Sale {sale.InvoiceNumber} completed - {_regional.FormatCurrency(sale.TotalAmount)}";

        TransitionBillingSession(BillingSessionState.Completed);
        CartItems.Clear();
        SelectedCartItem = null;
        SelectedCustomer = null;
        ResetPayment();
        UpdateCartCommandStates();
        RecalculateTotals();
    });

    // ═══════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════

    private void RecalculateTotals()
    {
        Subtotal = CartItems.Sum(c => c.TaxableAmount);
        TotalTax = CartItems.Sum(c => c.LineTaxAmount);
        TotalCgst = CartItems.Sum(c => c.CgstAmount);
        TotalSgst = CartItems.Sum(c => c.SgstAmount);

        _ = decimal.TryParse(DiscountInput, out var discVal);
        DiscountAmount = SelectedDiscountType switch
        {
            DiscountType.Amount => Math.Min(discVal, Subtotal),
            DiscountType.Percentage => Math.Min(Subtotal * discVal / 100m, Subtotal),
            _ => 0
        };

        GrandTotal = Math.Max(0, Subtotal + TotalTax - DiscountAmount);
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

    private bool CanRemoveCartItem() => SelectedCartItem is not null;

    private bool CanClearCart() => CartItems.Count > 0;

    private bool CanCompleteSale() => CartItems.Count > 0;

    private void UpdateCartCommandStates()
    {
        RemoveCartItemCommand.NotifyCanExecuteChanged();
        ClearCartCommand.NotifyCanExecuteChanged();
        CompleteSaleCommand.NotifyCanExecuteChanged();
    }

    private void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private bool CanAddProductToCart(Product? product) => product is not null;

    private bool IsValidCart() =>
        CartItems.All(item =>
            item.Quantity > 0
            && item.UnitPrice >= 0
            && item.ItemDiscountRate is >= 0 and <= 100
            && item.ItemDiscountAmount >= 0);

    private bool TryParseDiscountValue(out decimal discountValue, out string? errorMessage)
    {
        discountValue = 0;
        errorMessage = null;

        if (SelectedDiscountType == DiscountType.None)
            return true;

        if (!decimal.TryParse(DiscountInput, out discountValue) || discountValue < 0)
        {
            errorMessage = "Enter a valid discount value.";
            return false;
        }

        if (SelectedDiscountType == DiscountType.Percentage && discountValue > 100)
        {
            errorMessage = "Discount percentage must be between 0 and 100.";
            return false;
        }

        return true;
    }

    public override void Dispose()
    {
        TransitionBillingSession(BillingSessionState.None);

        if (_trackedCartItems is not null)
        {
            DetachCartCollection(_trackedCartItems);
            _trackedCartItems = null;
        }

        base.Dispose();
    }

    private void AttachCartCollection(ObservableCollection<CartLineViewModel> items)
    {
        items.CollectionChanged -= OnCartItemsCollectionChanged;
        items.CollectionChanged += OnCartItemsCollectionChanged;

        foreach (var line in items)
        {
            AttachCartLine(line);
        }
    }

    private void DetachCartCollection(ObservableCollection<CartLineViewModel> items)
    {
        items.CollectionChanged -= OnCartItemsCollectionChanged;

        foreach (var line in items)
        {
            DetachCartLine(line);
        }
    }

    private void AttachCartLine(CartLineViewModel line)
    {
        if (!_trackedCartLines.Add(line))
        {
            return;
        }

        line.PropertyChanged += OnCartLinePropertyChanged;
    }

    private void DetachCartLine(CartLineViewModel line)
    {
        if (!_trackedCartLines.Remove(line))
        {
            return;
        }

        line.PropertyChanged -= OnCartLinePropertyChanged;
    }

    private void OnCartItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            SyncCartLineSubscriptions();
        }
        else
        {
            if (e.OldItems is not null)
            {
                foreach (CartLineViewModel line in e.OldItems)
                {
                    DetachCartLine(line);
                }
            }

            if (e.NewItems is not null)
            {
                foreach (CartLineViewModel line in e.NewItems)
                {
                    AttachCartLine(line);
                }
            }
        }

        if (SelectedCartItem is not null && !CartItems.Contains(SelectedCartItem))
        {
            SelectedCartItem = null;
        }

        UpdateCartCommandStates();
        RecalculateTotals();
        HandleCartEmptiedState();
    }

    private void SyncCartLineSubscriptions()
    {
        foreach (var line in _trackedCartLines.ToArray())
        {
            if (!CartItems.Contains(line))
            {
                DetachCartLine(line);
            }
        }

        foreach (var line in CartItems)
        {
            AttachCartLine(line);
        }
    }

    private void OnCartLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName == nameof(CartLineViewModel.UnitPrice)
            || e.PropertyName == nameof(CartLineViewModel.Quantity)
            || e.PropertyName == nameof(CartLineViewModel.ItemDiscountRate)
            || e.PropertyName == nameof(CartLineViewModel.LineTotal))
        {
            RecalculateTotals();
        }
    }

    private void HandleCartEmptiedState()
    {
        if (CartItems.Count != 0 || _sessionState != BillingSessionState.Active)
            return;

        TransitionBillingSession(BillingSessionState.Cancelled);
        ResetPayment();
    }

    private void TransitionBillingSession(BillingSessionState state)
    {
        if (_sessionState == state)
            return;

        _sessionState = state;
        _appState.SetBillingSession(state);
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
    [NotifyPropertyChangedFor(nameof(LineTaxAmount))]
    [NotifyPropertyChangedFor(nameof(TaxableAmount))]
    public partial decimal UnitPrice { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [NotifyPropertyChangedFor(nameof(LineTaxAmount))]
    [NotifyPropertyChangedFor(nameof(TaxableAmount))]
    public partial int Quantity { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [NotifyPropertyChangedFor(nameof(LineTaxAmount))]
    [NotifyPropertyChangedFor(nameof(TaxableAmount))]
    public partial decimal ItemDiscountRate { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LineTotal))]
    [NotifyPropertyChangedFor(nameof(LineTaxAmount))]
    [NotifyPropertyChangedFor(nameof(TaxableAmount))]
    public partial decimal ItemDiscountAmount { get; set; }

    public decimal TaxRate { get; set; }

    public bool IsTaxInclusive { get; set; }

    public decimal TaxableAmount
    {
        get
        {
            var subtotal = Quantity * UnitPrice;
            var percentDisc = subtotal * ItemDiscountRate / 100m;
            return Math.Max(0, subtotal - percentDisc - ItemDiscountAmount);
        }
    }

    public decimal LineTaxAmount
    {
        get
        {
            if (TaxRate <= 0) return 0;
            return IsTaxInclusive
                ? TaxableAmount - TaxableAmount / (1 + TaxRate / 100m)
                : TaxableAmount * TaxRate / 100m;
        }
    }

    /// <summary>CGST component (intra-state = LineTaxAmount / 2).</summary>
    public decimal CgstAmount => LineTaxAmount / 2m;

    /// <summary>SGST component (intra-state = LineTaxAmount / 2).</summary>
    public decimal SgstAmount => LineTaxAmount / 2m;

    /// <summary>CGST rate (half of GST rate).</summary>
    public decimal CgstRate => TaxRate / 2m;

    /// <summary>SGST rate (half of GST rate).</summary>
    public decimal SgstRate => TaxRate / 2m;

    public decimal LineTotal => IsTaxInclusive
        ? TaxableAmount
        : TaxableAmount + LineTaxAmount;
}

