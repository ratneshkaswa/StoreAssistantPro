using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Billing.Events;
using StoreAssistantPro.Modules.Billing.Services;

namespace StoreAssistantPro.Modules.Billing.ViewModels;

public partial class SaleHistoryViewModel(
    ISaleHistoryService historyService,
    IReceiptService receiptService,
    IRegionalSettingsService regional,
    IEventBus eventBus) : BaseViewModel
{
    private static readonly TimeSpan NavigationFreshnessWindow = TimeSpan.FromMinutes(2);
    private bool _isRestoringViewState;
    private bool _salesChangeSubscribed;

    // ── Filters ──

    [ObservableProperty]
    public partial DateTime? DateFrom { get; set; }

    [ObservableProperty]
    public partial DateTime? DateTo { get; set; }

    [ObservableProperty]
    public partial string InvoiceSearch { get; set; } = string.Empty;

    partial void OnDateFromChanged(DateTime? value) => PersistViewState();

    partial void OnDateToChanged(DateTime? value) => PersistViewState();

    partial void OnInvoiceSearchChanged(string value) => PersistViewState();

    // ── Paging ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    public partial int CurrentPage { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    public partial int TotalPages { get; set; } = 1;

    [ObservableProperty]
    public partial int TotalCount { get; set; }

    [ObservableProperty]
    public partial string PagingInfo { get; set; } = string.Empty;

    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    private const int PageSize = 25;

    // ── Data ──

    [ObservableProperty]
    public partial ObservableCollection<Sale> Sales { get; set; } = [];

    [ObservableProperty]
    public partial Sale? SelectedSale { get; set; }

    [ObservableProperty]
    public partial string SaleDetail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReceiptPreview { get; set; } = string.Empty;

    partial void OnSelectedSaleChanged(Sale? value)
    {
        if (value is null)
        {
            SaleDetail = string.Empty;
            ReceiptPreview = string.Empty;
            return;
        }

        SaleDetail = $"Invoice: {value.InvoiceNumber}\n" +
                     $"Date: {regional.FormatDate(value.SaleDate)} {regional.FormatTime(value.SaleDate)}\n" +
                     $"Items: {value.Items.Count}\n" +
                     $"Total: {regional.FormatCurrency(value.TotalAmount)}\n" +
                     $"Payment: {value.PaymentMethod}" +
                     (string.IsNullOrEmpty(value.PaymentReference) ? "" : $" ({value.PaymentReference})") +
                     (value.DiscountAmount > 0 ? $"\nDiscount: {regional.FormatCurrency(value.DiscountAmount)}" : "");
    }

    // ── Commands ──

    [RelayCommand]
    private Task LoadAsync() => LoadOnActivateAsync(async ct =>
    {
        EnsureSalesChangeSubscription();
        RestoreViewState();
        await ReloadAsync(ct);
    }, NavigationFreshnessWindow);

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        CurrentPage = 1;
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task PreviousPageAsync() => RunAsync(async ct =>
    {
        if (!HasPreviousPage) return;
        CurrentPage--;
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task NextPageAsync() => RunAsync(async ct =>
    {
        if (!HasNextPage) return;
        CurrentPage++;
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task PreviewReceiptAsync() => RunAsync(async ct =>
    {
        ErrorMessage = string.Empty;
        if (SelectedSale is null)
        {
            ErrorMessage = "Select a sale first.";
            return;
        }

        var receipt = await receiptService.GenerateThermalReceiptAsync(SelectedSale.Id, ct);
        ReceiptPreview = receipt;
    });

    [RelayCommand]
    private void ExportCsv()
    {
        if (Sales.Count == 0) return;
        if (CsvExporter.Export(Sales, "SaleHistory.csv"))
            SuccessMessage = "Exported to CSV.";
    }

    private async Task ReloadAsync(CancellationToken ct)
    {
        var invoice = string.IsNullOrWhiteSpace(InvoiceSearch) ? null : InvoiceSearch;
        var result = await historyService.GetPagedAsync(new PagedQuery(CurrentPage, PageSize), DateFrom, DateTo, invoice, ct);
        Sales = new ObservableCollection<Sale>(result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        PagingInfo = TotalCount > 0
            ? $"Page {CurrentPage} of {TotalPages} ({TotalCount} total)"
            : string.Empty;
        MarkLoadCompleted();
    }

    private void RestoreViewState()
    {
        _isRestoringViewState = true;
        try
        {
            var state = UserPreferencesStore.GetSaleHistoryState();
            DateFrom = state.DateFrom;
            DateTo = state.DateTo;
            InvoiceSearch = state.InvoiceSearch;
        }
        finally
        {
            _isRestoringViewState = false;
        }
    }

    private void PersistViewState()
    {
        if (_isRestoringViewState)
            return;

        UserPreferencesStore.SetSaleHistoryState(new SaleHistoryViewState
        {
            DateFrom = DateFrom,
            DateTo = DateTo,
            InvoiceSearch = InvoiceSearch
        });
    }

    public override void Dispose()
    {
        if (_salesChangeSubscribed)
        {
            eventBus.Unsubscribe<SalesDataChangedEvent>(HandleSalesDataChangedAsync);
            _salesChangeSubscribed = false;
        }

        base.Dispose();
    }

    private void EnsureSalesChangeSubscription()
    {
        if (_salesChangeSubscribed)
            return;

        eventBus.Subscribe<SalesDataChangedEvent>(HandleSalesDataChangedAsync);
        _salesChangeSubscribed = true;
    }

    private Task HandleSalesDataChangedAsync(SalesDataChangedEvent _)
    {
        historyService.InvalidateCache();
        MarkLoadStale();
        return Task.CompletedTask;
    }
}
