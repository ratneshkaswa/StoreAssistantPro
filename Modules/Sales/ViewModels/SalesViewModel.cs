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
    private int _pageSize = 50;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (_pageSize != value)
            {
                _pageSize = value;
                OnPropertyChanged();
                PageIndex = 0;
                _ = LoadCurrentPageAsync();
            }
        }
    }

    public int[] PageSizeOptions { get; } = new[] { 25, 50, 100, 200 };
    private bool _isDateFiltered;

    // ── Role-based access ──

    public bool CanCreateSales =>
        sessionService.CurrentUserType is UserType.Admin or UserType.Manager;


    // ── Search ──
    private IReadOnlyList<Sale> _allSales = [];

    [ObservableProperty]
    public partial ObservableCollection<Sale> Sales { get; set; } = [];

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    // ── Payment method filter ──

    [ObservableProperty]
    public partial string? SelectedPaymentMethod { get; set; }

    partial void OnSelectedPaymentMethodChanged(string? value) => ApplyFilter();

    public string[] PaymentMethodFilterOptions { get; } = ["All", "Cash", "Card", "UPI", "Transfer"];

    // ── Cashier filter ──

    [ObservableProperty]
    public partial string? SelectedCashierFilter { get; set; }

    partial void OnSelectedCashierFilterChanged(string? value) => ApplyFilter();

    public string[] CashierFilterOptions { get; } = ["All", "Admin", "Manager", "User"];

    // ── Sorting ──

    [ObservableProperty]
    public partial string? SortColumn { get; set; }

    [ObservableProperty]
    public partial bool SortDescending { get; set; }

    [RelayCommand]
    private Task SortByColumnAsync(string columnName)
    {
        if (string.Equals(SortColumn, columnName, StringComparison.OrdinalIgnoreCase))
        {
            SortDescending = !SortDescending;
        }
        else
        {
            SortColumn = columnName;
            SortDescending = false;
        }
        PageIndex = 0;
        return LoadCurrentPageAsync();
    }

    private void ApplyFilter()
    {
        IEnumerable<Sale> filtered = _allSales;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(s =>
                (!string.IsNullOrEmpty(s.InvoiceNumber) && s.InvoiceNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrEmpty(s.PaymentMethod) && s.PaymentMethod.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrEmpty(s.CashierRole) && s.CashierRole.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            );
        }
        if (!string.IsNullOrWhiteSpace(SelectedPaymentMethod) && SelectedPaymentMethod != "All")
        {
            filtered = filtered.Where(s => s.PaymentMethod == SelectedPaymentMethod);
        }
        if (!string.IsNullOrWhiteSpace(SelectedCashierFilter) && SelectedCashierFilter != "All")
        {
            filtered = filtered.Where(s =>
                !string.IsNullOrEmpty(s.CashierRole) && s.CashierRole.Equals(SelectedCashierFilter, StringComparison.OrdinalIgnoreCase));
        }
        Sales = new ObservableCollection<Sale>(filtered.ToList());
    }

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

    public string[] PaymentMethods { get; } = ["Cash", "Card", "UPI", "Transfer"];

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
            new PagedQuery(PageIndex, PageSize, null, StockFilter.All, ActiveFilter.All, null, SortColumn, SortDescending), from, to, ct);

        _allSales = result.Items;
        ApplyFilter();
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

    // ── Export ──

    [RelayCommand]
    private async Task ExportSalesAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Export Sales to CSV",
            FileName = "Sales_Export.csv"
        };

        if (dialog.ShowDialog() != true) return;

        ErrorMessage = string.Empty;

        try
        {
            var allSales = await salesService.GetAllAsync();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("InvoiceNumber,Date,Total,Payment,Cashier,ItemCount");

            foreach (var s in allSales)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(s.InvoiceNumber),
                    s.SaleDate.ToString("g", System.Globalization.CultureInfo.InvariantCulture),
                    s.TotalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    EscapeCsv(s.PaymentMethod ?? ""),
                    EscapeCsv(s.CashierRole ?? ""),
                    s.Items?.Count ?? 0));
            }

            await System.IO.File.WriteAllTextAsync(dialog.FileName, sb.ToString());
            ErrorMessage = $"Exported {allSales.Count()} sales to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ExportFilteredSalesAsync()
    {
        if (Sales.Count == 0)
        {
            ErrorMessage = "No filtered sales to export.";
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Export Filtered Sales to CSV",
            FileName = "Sales_Filtered_Export.csv"
        };

        if (dialog.ShowDialog() != true) return;

        ErrorMessage = string.Empty;

        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("InvoiceNumber,Date,Total,Payment,Cashier,ItemCount");

            foreach (var s in Sales)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(s.InvoiceNumber),
                    s.SaleDate.ToString("g", System.Globalization.CultureInfo.InvariantCulture),
                    s.TotalAmount.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    EscapeCsv(s.PaymentMethod ?? ""),
                    EscapeCsv(s.CashierRole ?? ""),
                    s.Items?.Count ?? 0));
            }

            await System.IO.File.WriteAllTextAsync(dialog.FileName, sb.ToString());
            ErrorMessage = $"Exported {Sales.Count} filtered sales to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    // ── Bill recalculation ──

    private void RecalculateBill()
    {
        var discount = BuildDiscount();

        if (discount.Type == DiscountType.Percentage && discount.Value > 100)
        {
            ErrorMessage = "Discount percentage cannot exceed 100%.";
            BillDiscountAmount = 0;
            BillFinalAmount = CartTotal;
            OnPropertyChanged(nameof(HasDiscount));
            return;
        }

        if (discount.Type == DiscountType.Amount && discount.Value > CartTotal)
        {
            ErrorMessage = "Discount amount cannot exceed the subtotal.";
            BillDiscountAmount = 0;
            BillFinalAmount = CartTotal;
            OnPropertyChanged(nameof(HasDiscount));
            return;
        }

        ErrorMessage = string.Empty;

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

    // ── Sale detail panel actions ──

    [RelayCommand]
    private void PrintInvoice()
    {
        if (SelectedSale is null)
        {
            ErrorMessage = "No sale selected.";
            return;
        }
        // TODO: Replace with real print logic
        ErrorMessage = $"Print invoice for {SelectedSale.InvoiceNumber ?? SelectedSale.Id.ToString()} (stub).";
    }

    [RelayCommand]
    private void RefundSale()
    {
        if (SelectedSale is null)
        {
            ErrorMessage = "No sale selected.";
            return;
        }
        // TODO: Replace with real refund workflow
        ErrorMessage = $"Refund workflow for {SelectedSale.InvoiceNumber ?? SelectedSale.Id.ToString()} (stub).";
    }

    [RelayCommand]
    private async Task ExportSaleItemsAsync()
    {
        if (SelectedSale is null)
        {
            ErrorMessage = "No sale selected.";
            return;
        }
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Export Sale Items to CSV",
            FileName = $"Sale_{SelectedSale.InvoiceNumber ?? SelectedSale.Id.ToString()}_Items.csv"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Product,UnitPrice,Quantity,Subtotal");
            foreach (var item in SelectedSale.Items)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(item.Product?.Name ?? ""),
                    item.UnitPrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    item.Quantity,
                    (item.UnitPrice * item.Quantity).ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }
            await System.IO.File.WriteAllTextAsync(dialog.FileName, sb.ToString());
            ErrorMessage = $"Exported {SelectedSale.Items.Count} items to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }

    // ── Toolbar actions: Reprint Last, Export All Items ──
    [RelayCommand]
    private void ReprintLastInvoice()
    {
        var last = Sales.OrderByDescending(s => s.SaleDate).FirstOrDefault();
        if (last is null)
        {
            ErrorMessage = "No sales to reprint.";
            return;
        }
        // TODO: Replace with real print logic
        ErrorMessage = $"Reprint invoice for {last.InvoiceNumber ?? last.Id.ToString()} (stub).";
    }

    [RelayCommand]
    private async Task ExportAllSaleItemsAsync()
    {
        if (Sales.Count == 0)
        {
            ErrorMessage = "No sales to export.";
            return;
        }
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Export All Sale Items to CSV",
            FileName = "AllSaleItems_Export.csv"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("InvoiceNumber,Date,Product,UnitPrice,Quantity,Subtotal");
            foreach (var sale in Sales)
            {
                foreach (var item in sale.Items)
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(sale.InvoiceNumber),
                        sale.SaleDate.ToString("g", System.Globalization.CultureInfo.InvariantCulture),
                        EscapeCsv(item.Product?.Name ?? ""),
                        item.UnitPrice.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        item.Quantity,
                        (item.UnitPrice * item.Quantity).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }
            }
            await System.IO.File.WriteAllTextAsync(dialog.FileName, sb.ToString());
            ErrorMessage = $"Exported all sale items to {System.IO.Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Export failed: {ex.Message}";
        }
    }
}
