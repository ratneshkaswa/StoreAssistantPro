using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Data;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Sales.Commands;
using StoreAssistantPro.Modules.Sales.Services;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Core.Session;

namespace StoreAssistantPro.Modules.Sales.ViewModels;

public partial class SalesViewModel(
    ISalesService salesService,
    IProductService productService,
    ISessionService sessionService,
    ICommandBus commandBus,
    IBillCalculationService billCalculation,
    IRegionalSettingsService regional) : BaseViewModel
{
    private const int PageSize = 50;
    private bool _isDateFiltered;

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
    public partial DateTime FilterFrom { get; set; }

    [ObservableProperty]
    public partial DateTime FilterTo { get; set; }

    // ── Paging state ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int PageIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    public partial int TotalPages { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PageDisplay))]
    public partial int TotalCount { get; set; }

    public string PageDisplay => TotalPages > 0
        ? $"Page {PageIndex + 1} of {TotalPages} ({TotalCount} items)"
        : "No results";

    public bool HasPreviousPage => PageIndex > 0;
    public bool HasNextPage => PageIndex < TotalPages - 1;

    // ── New Sale form ──
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

    // ── Bill-level discount (optional) ──

    public DiscountType[] DiscountTypes { get; } =
        [DiscountType.None, DiscountType.Amount, DiscountType.Percentage];

    [ObservableProperty]
    public partial DiscountType SelectedDiscountType { get; set; }

    [ObservableProperty]
    public partial decimal DiscountInput { get; set; }

    [ObservableProperty]
    public partial string DiscountReason { get; set; }

    // ── Computed bill summary (driven by RecalculateBill) ──

    [ObservableProperty]
    public partial decimal BillDiscountAmount { get; set; }

    [ObservableProperty]
    public partial decimal BillFinalAmount { get; set; }

    /// <summary>
    /// <c>true</c> while a sale save is in progress.
    /// Disables all cart-editing controls and the Complete Sale button,
    /// and shows a processing indicator in the billing form.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCartLocked))]
    [NotifyPropertyChangedFor(nameof(SavingStatusText))]
    [NotifyCanExecuteChangedFor(nameof(CompleteSaleCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddToCartCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveFromCartCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelNewSaleCommand))]
    [NotifyCanExecuteChangedFor(nameof(ShowNewSaleCommand))]
    public partial bool IsSaving { get; set; }

    /// <summary>
    /// <c>true</c> when the billing form inputs should be disabled.
    /// Bound to <c>IsEnabled</c> (via inverse) on all cart controls.
    /// </summary>
    public bool IsCartLocked => IsSaving;

    /// <summary>
    /// Text shown in the processing indicator overlay.
    /// Empty when not saving (overlay hidden).
    /// </summary>
    public string SavingStatusText => IsSaving ? "Processing sale…" : string.Empty;

    public bool HasDiscount => SelectedDiscountType != DiscountType.None && DiscountInput > 0;

    partial void OnSelectedDiscountTypeChanged(DiscountType value) => RecalculateBill();

    partial void OnDiscountInputChanged(decimal value) => RecalculateBill();

    [RelayCommand]
    private Task LoadSalesAsync()
    {
        _isDateFiltered = false;
        PageIndex = 0;

        var today = regional.Now.Date;
        FilterFrom = today;
        FilterTo = today;

        return LoadCurrentPageAsync();
    }

    [RelayCommand]
    private Task FilterByDateAsync()
    {
        if (FilterFrom > FilterTo)
        {
            ErrorMessage = "\"From\" date cannot be after \"To\" date.";
            return Task.CompletedTask;
        }

        _isDateFiltered = true;
        PageIndex = 0;
        return LoadCurrentPageAsync();
    }

    private Task LoadCurrentPageAsync() => RunLoadAsync(async ct =>
    {
        var from = _isDateFiltered ? (DateTime?)FilterFrom.Date : null;
        var to = _isDateFiltered ? (DateTime?)FilterTo.Date.AddDays(1) : null;

        var result = await salesService.GetPagedAsync(
            new PagedQuery(PageIndex, PageSize), from, to, ct);

        Sales = new ObservableCollection<Sale>(result.Items);
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
        PageIndex = result.PageIndex;
    });

    [RelayCommand(CanExecute = nameof(HasNextPage))]
    private Task NextPageAsync()
    {
        PageIndex++;
        return LoadCurrentPageAsync();
    }

    [RelayCommand(CanExecute = nameof(HasPreviousPage))]
    private Task PreviousPageAsync()
    {
        PageIndex--;
        return LoadCurrentPageAsync();
    }

    [RelayCommand(CanExecute = nameof(CanEditCart))]
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
        SelectedDiscountType = DiscountType.None;
        DiscountInput = 0;
        DiscountReason = string.Empty;
        BillDiscountAmount = 0;
        BillFinalAmount = 0;
        IsNewSaleVisible = true;
    }

    [RelayCommand(CanExecute = nameof(CanEditCart))]
    private void CancelNewSale()
    {
        IsNewSaleVisible = false;
    }

    private bool CanEditCart() => !IsSaving;

    [RelayCommand(CanExecute = nameof(CanEditCart))]
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
            existing.UnitPrice = SelectedProduct.SalePrice;
            CartItems = new ObservableCollection<SaleItem>(CartItems);
        }
        else
        {
            CartItems.Add(new SaleItem
            {
                ProductId = SelectedProduct.Id,
                Product = SelectedProduct,
                Quantity = CartQuantity,
                UnitPrice = SelectedProduct.SalePrice
            });
        }

        CartTotal = CartItems.Sum(i => i.Subtotal);
        RecalculateBill();
        CartQuantity = 1;
        SelectedProduct = null;
    }

    [RelayCommand(CanExecute = nameof(CanEditCart))]
    private void RemoveFromCart(SaleItem? item)
    {
        if (item is null) return;

        CartItems.Remove(item);
        CartTotal = CartItems.Sum(i => i.Subtotal);
        RecalculateBill();
    }

    [RelayCommand(CanExecute = nameof(CanCompleteSale))]
    private async Task CompleteSaleAsync()
    {
        ErrorMessage = string.Empty;

        if (CartItems.Count == 0) return;

        IsSaving = true;
        try
        {
            var idempotencyKey = Guid.NewGuid();

            var items = CartItems.Select(i =>
                new SaleItemDto(i.ProductId, i.Quantity, i.UnitPrice)).ToList();

            var discount = BuildDiscount();

            var result = await commandBus.SendAsync(
                new CompleteSaleCommand(idempotencyKey, BillFinalAmount, PaymentMethod, items, discount));

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
        finally
        {
            IsSaving = false;
        }
    }

    private bool CanCompleteSale() => !IsSaving;

    // ── Bill recalculation ──

    private void RecalculateBill()
    {
        var discount = BuildDiscount();

        try
        {
            var summary = billCalculation.Calculate(CartTotal, 0m, discount);
            BillDiscountAmount = summary.DiscountAmount;
            BillFinalAmount = summary.FinalAmount;
        }
        catch (ArgumentOutOfRangeException)
        {
            BillDiscountAmount = 0;
            BillFinalAmount = CartTotal;
        }

        OnPropertyChanged(nameof(HasDiscount));
    }

    private BillDiscount BuildDiscount() => SelectedDiscountType == DiscountType.None
        ? BillDiscount.None
        : new BillDiscount
        {
            Type = SelectedDiscountType,
            Value = DiscountInput,
            Reason = string.IsNullOrWhiteSpace(DiscountReason) ? null : DiscountReason.Trim()
        };
}
