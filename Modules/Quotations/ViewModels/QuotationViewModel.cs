using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Customers.Services;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Quotations.Services;

namespace StoreAssistantPro.Modules.Quotations.ViewModels;

public partial class QuotationViewModel(
    IQuotationService quotationService,
    ICustomerService customerService,
    IProductService productService,
    IDialogService dialogService,
    IRegionalSettingsService regional) : BaseViewModel
{
    private static readonly TimeSpan NavigationFreshnessWindow = TimeSpan.FromMinutes(2);
    private bool _isRestoringViewState;

    // ── List ──

    [ObservableProperty]
    public partial ObservableCollection<Quotation> Quotations { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedQuotation))]
    [NotifyPropertyChangedFor(nameof(CanAccept))]
    [NotifyPropertyChangedFor(nameof(CanReject))]
    [NotifyPropertyChangedFor(nameof(CanConvert))]
    [NotifyPropertyChangedFor(nameof(CanMarkSent))]
    public partial Quotation? SelectedQuotation { get; set; }

    public bool HasSelectedQuotation => SelectedQuotation is not null;
    public bool CanAccept => SelectedQuotation is { Status: QuotationStatus.Draft or QuotationStatus.Sent };
    public bool CanReject => SelectedQuotation is { Status: QuotationStatus.Draft or QuotationStatus.Sent };
    public bool CanConvert => SelectedQuotation is { Status: QuotationStatus.Accepted };
    public bool CanMarkSent => SelectedQuotation is { Status: QuotationStatus.Draft };

    // ── Search / filter ──

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    partial void OnSearchQueryChanged(string value) => PersistViewState();

    [ObservableProperty]
    public partial QuotationStatus? FilterStatus { get; set; }

    partial void OnFilterStatusChanged(QuotationStatus? value) => PersistViewState();

    public ObservableCollection<QuotationStatus?> StatusOptions { get; } =
    [
        null,
        QuotationStatus.Draft,
        QuotationStatus.Sent,
        QuotationStatus.Accepted,
        QuotationStatus.Rejected,
        QuotationStatus.Expired,
        QuotationStatus.ConvertedToSale
    ];

    // ── Paging (#354) ──

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPreviousPage))]
    [NotifyPropertyChangedFor(nameof(HasNextPage))]
    public partial int CurrentPage { get; set; } = 1;

    partial void OnCurrentPageChanged(int value) => PersistViewState();

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

    // ── Create form ──

    [ObservableProperty]
    public partial ObservableCollection<Customer> Customers { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<Product> Products { get; set; } = [];

    [ObservableProperty]
    public partial Customer? SelectedCustomer { get; set; }

    [ObservableProperty]
    public partial DateTime ValidUntilDate { get; set; } = DateTime.Today.AddDays(15);

    [ObservableProperty]
    public partial string Notes { get; set; } = string.Empty;

    [ObservableProperty]
    public partial ObservableCollection<QuotationLineInput> LineItems { get; set; } = [];

    // ── Load ──

    [RelayCommand]
    private Task LoadAsync() => LoadOnActivateAsync(async ct =>
    {
        RestoreViewState();
        var customersTask = customerService.GetActiveAsync(ct);
        var productsTask = productService.GetActiveAsync(ct);

        await Task.WhenAll(customersTask, productsTask);

        Customers = new ObservableCollection<Customer>(customersTask.Result);
        Products = new ObservableCollection<Product>(productsTask.Result);

        // Expire overdue quotations on load (#355)
        await quotationService.ExpireOverdueAsync(ct);

        await ReloadAsync(ct);
        EnsureSeedLine();
    },
        NavigationFreshnessWindow);

    // ── Search (#353) ──

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        CurrentPage = 1;
        await ReloadAsync(ct);
    });

    // ── Paging (#354) ──

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

    private async Task ReloadAsync(CancellationToken ct)
    {
        var search = string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery;
        var result = await quotationService.GetPagedAsync(
            new PagedQuery(CurrentPage, PageSize), search, FilterStatus, null, null, ct);
        Quotations = new ObservableCollection<Quotation>(result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        PagingInfo = TotalCount > 0
            ? $"Page {CurrentPage} of {TotalPages} ({TotalCount} total)"
            : string.Empty;
    }

    // ── Create (#349) ──

    [RelayCommand]
    private Task CreateQuotationAsync() => RunAsync(async ct =>
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        var enteredItems = LineItems
            .Where(line => line.ProductId > 0 || line.Quantity > 0)
            .ToList();

        if (!Validate(v => v
            .Rule(enteredItems.Count > 0, "Add at least one item.")
            .Rule(ValidUntilDate > regional.Now.Date, "Validity date must be in the future.")))
        {
            return;
        }

        var invalidLine = enteredItems
            .Select((line, index) => new { line, index })
            .FirstOrDefault(x => x.line.ProductId <= 0 || x.line.Quantity <= 0 || x.line.UnitPrice <= 0);

        if (invalidLine is not null)
        {
            ErrorMessage = $"Complete line {invalidLine.index + 1} with product, quantity, and price.";
            return;
        }

        var dto = new CreateQuotationDto(
            SelectedCustomer?.Id,
            ValidUntilDate,
            string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
            enteredItems.Select(l => new QuotationLineDto(
                l.ProductId, l.Quantity, l.UnitPrice,
                l.DiscountRate, l.TaxRate, l.CessRate)).ToList());

        var qt = await quotationService.CreateAsync(dto, ct);
        SuccessMessage = $"Quotation {qt.QuoteNumber} created.";

        SelectedCustomer = null;
        ValidUntilDate = DateTime.Today.AddDays(15);
        Notes = string.Empty;
        LineItems.Clear();
        EnsureSeedLine();

        await ReloadAsync(ct);
    });

    // ── Status workflow (#352) ──

    [RelayCommand]
    private Task MarkSentAsync() => RunAsync(async ct =>
    {
        if (SelectedQuotation is null) return;
        await quotationService.UpdateStatusAsync(SelectedQuotation.Id, QuotationStatus.Sent, ct);
        SuccessMessage = $"Quotation {SelectedQuotation.QuoteNumber} marked as Sent.";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task AcceptQuotationAsync() => RunAsync(async ct =>
    {
        if (SelectedQuotation is null) return;
        await quotationService.UpdateStatusAsync(SelectedQuotation.Id, QuotationStatus.Accepted, ct);
        SuccessMessage = $"Quotation {SelectedQuotation.QuoteNumber} accepted.";
        await ReloadAsync(ct);
    });

    [RelayCommand]
    private Task RejectQuotationAsync() => RunAsync(async ct =>
    {
        if (SelectedQuotation is null) return;
        if (!dialogService.Confirm("Reject this quotation?", "Reject")) return;
        await quotationService.UpdateStatusAsync(SelectedQuotation.Id, QuotationStatus.Rejected, ct);
        SuccessMessage = $"Quotation {SelectedQuotation.QuoteNumber} rejected.";
        await ReloadAsync(ct);
    });

    // ── Convert to Sale (#351) ──

    [RelayCommand]
    private Task ConvertToSaleAsync() => RunAsync(async ct =>
    {
        if (SelectedQuotation is null) return;
        if (!dialogService.Confirm(
            $"Convert quotation {SelectedQuotation.QuoteNumber} to a sale?\n\nThis will open the billing page with the quotation items.",
            "Convert to Sale"))
            return;

        // Get cart lines — the billing module will pick these up
        var lines = await quotationService.GetCartLinesAsync(SelectedQuotation.Id, ct);
        QuotationToConvert = SelectedQuotation;
        CartLinesForConversion = lines;
        SuccessMessage = $"Quotation {SelectedQuotation.QuoteNumber} ready for billing. Navigate to Billing to complete.";
    });

    /// <summary>Set when a quotation is being converted — consumed by BillingViewModel.</summary>
    [ObservableProperty]
    public partial Quotation? QuotationToConvert { get; set; }

    /// <summary>Cart lines for conversion — consumed by BillingViewModel.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<QuotationCartLine>? CartLinesForConversion { get; set; }

    // ── Line items ──

    private void EnsureSeedLine()
    {
        if (LineItems.Count == 0)
            LineItems.Add(new QuotationLineInput());
    }

    [RelayCommand]
    private void AddLine()
    {
        LineItems.Add(new QuotationLineInput());
    }

    [RelayCommand]
    private void RemoveLine(QuotationLineInput? line)
    {
        if (line is not null && LineItems.Count > 1)
            LineItems.Remove(line);
    }

    // ── Duplicate (#357) ──

    [RelayCommand]
    private Task DuplicateQuotationAsync() => RunAsync(async ct =>
    {
        if (SelectedQuotation is null) return;
        var clone = await quotationService.DuplicateAsync(
            SelectedQuotation.Id, DateTime.Today.AddDays(15), ct);
        SuccessMessage = $"Quotation duplicated as {clone.QuoteNumber} (Draft).";
        await ReloadAsync(ct);
    });

    // ── Revision (#356) ──

    [RelayCommand]
    private Task CreateRevisionAsync() => RunAsync(async ct =>
    {
        if (SelectedQuotation is null) return;
        var revision = await quotationService.CreateRevisionAsync(
            SelectedQuotation.Id, DateTime.Today.AddDays(15), ct);
        SuccessMessage = $"Revision {revision.RevisionNumber} created as {revision.QuoteNumber}.";
        await ReloadAsync(ct);
    });

    // ── Export (#359) ──

    [RelayCommand]
    private void ExportQuotationsCsv()
    {
        if (Quotations.Count == 0) return;
        var rows = Quotations.Select(q => new
        {
            q.Id, q.QuoteNumber, q.QuoteDate, q.ValidUntil,
            Status = q.Status.ToString(),
            Customer = q.Customer?.Name,
            q.TotalAmount, q.Notes
        });
        if (CsvExporter.Export(rows, "Quotations.csv"))
            SuccessMessage = "Quotations exported.";
    }

    private void RestoreViewState()
    {
        _isRestoringViewState = true;
        try
        {
            var state = UserPreferencesStore.GetQuotationsState();
            SearchQuery = state.SearchText;
            CurrentPage = state.CurrentPage;
            FilterStatus = string.Equals(state.ActiveFilter, "All", StringComparison.OrdinalIgnoreCase)
                ? null
                : Enum.TryParse<QuotationStatus>(state.ActiveFilter, true, out var status)
                    ? status
                    : null;
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

        UserPreferencesStore.SetQuotationsState(new PagedSearchFilterViewState
        {
            SearchText = SearchQuery,
            ActiveFilter = FilterStatus?.ToString() ?? "All",
            CurrentPage = CurrentPage
        });
    }
}

/// <summary>Mutable input row for the quotation creation form.</summary>
public partial class QuotationLineInput : ObservableObject
{
    private static readonly TimeSpan NavigationFreshnessWindow = TimeSpan.FromMinutes(2);

    [ObservableProperty]
    public partial int ProductId { get; set; }

    [ObservableProperty]
    public partial int Quantity { get; set; }

    [ObservableProperty]
    public partial decimal UnitPrice { get; set; }

    [ObservableProperty]
    public partial decimal DiscountRate { get; set; }

    [ObservableProperty]
    public partial decimal TaxRate { get; set; }

    [ObservableProperty]
    public partial decimal CessRate { get; set; }
}
