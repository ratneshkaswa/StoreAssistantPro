using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core;
using StoreAssistantPro.Core.Helpers;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.GRN.Services;
using StoreAssistantPro.Modules.PurchaseOrders.Services;

namespace StoreAssistantPro.Modules.GRN.ViewModels;

public partial class GRNViewModel(
    IGRNService grnService,
    IPurchaseOrderService poService,
    IDialogService dialogService) : BaseViewModel
{

    private static readonly TimeSpan NavigationFreshnessWindow = TimeSpan.FromMinutes(2);
    private bool _isRestoringViewState;

    // ── List ──

    [ObservableProperty]
    public partial ObservableCollection<GoodsReceivedNote> GRNs { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedGRN))]
    [NotifyPropertyChangedFor(nameof(CanConfirm))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    public partial GoodsReceivedNote? SelectedGRN { get; set; }

    public bool HasSelectedGRN => SelectedGRN is not null;
    public bool CanConfirm => SelectedGRN is { Status: GRNStatus.Draft };
    public bool CanCancel => SelectedGRN is { Status: GRNStatus.Draft };

    // ── Search / filter (#369) ──

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    partial void OnSearchQueryChanged(string value) => PersistViewState();

    [ObservableProperty]
    public partial GRNStatus? FilterStatus { get; set; }

    partial void OnFilterStatusChanged(GRNStatus? value) => PersistViewState();

    public ObservableCollection<GRNStatus?> StatusOptions { get; } =
    [
        null,
        GRNStatus.Draft,
        GRNStatus.Confirmed,
        GRNStatus.Cancelled
    ];

    // ── Paging (#370) ──

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

    // ── Create from PO (#363) ──

    [ObservableProperty]
    public partial ObservableCollection<PurchaseOrder> PendingPOs { get; set; } = [];

    [ObservableProperty]
    public partial PurchaseOrder? SelectedPO { get; set; }

    [ObservableProperty]
    public partial string GRNNotes { get; set; } = string.Empty;

    // ── Direct GRN form (#364) ──

    [ObservableProperty]
    public partial ObservableCollection<Supplier> Suppliers { get; set; } = [];

    [ObservableProperty]
    public partial Supplier? SelectedSupplier { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<GRNLineInput> DirectLineItems { get; set; } = [];

    // ── Load ──

    [RelayCommand]
    private Task LoadAsync() => LoadOnActivateAsync(async ct =>
    {
        RestoreViewState();
        var posResult = await poService.GetPagedAsync(
            new PagedQuery(1, 100), status: PurchaseOrderStatus.Ordered, ct: ct);
        var partialResult = await poService.GetPagedAsync(
            new PagedQuery(1, 100), status: PurchaseOrderStatus.PartialReceived, ct: ct);

        PendingPOs = new ObservableCollection<PurchaseOrder>(
            posResult.Items.Concat(partialResult.Items).OrderByDescending(p => p.OrderDate));

        var suppliers = await poService.GetActiveSuppliersAsync(ct);
        Suppliers = new ObservableCollection<Supplier>(suppliers);

        EnsureDirectSeedLine();
        await ReloadAsync(ct);
    },
        NavigationFreshnessWindow);

    // ── Search (#369) ──

    [RelayCommand]
    private Task SearchAsync() => RunAsync(async ct =>
    {
        CurrentPage = 1;
        await ReloadAsync(ct);
    });

    // ── Paging (#370) ──

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
        var result = await grnService.GetPagedAsync(
            new PagedQuery(CurrentPage, PageSize), search, FilterStatus, null, null, ct);
        GRNs = new ObservableCollection<GoodsReceivedNote>(result.Items);
        TotalCount = result.TotalCount;
        TotalPages = result.TotalPages == 0 ? 1 : result.TotalPages;
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        PagingInfo = TotalCount > 0
            ? $"Page {CurrentPage} of {TotalPages} ({TotalCount} total)"
            : string.Empty;
    }

    // ── Create from PO (#363) ──

    [RelayCommand]
    private Task CreateFromPOAsync() => RunAsync(async ct =>
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (!Validate(v => v.Rule(SelectedPO is not null, "Select a purchase order.")))
            return;

        var grn = await grnService.CreateFromPOAsync(SelectedPO!.Id, GRNNotes.Trim(), ct);
        SuccessMessage = $"GRN {grn.GRNNumber} created from PO {SelectedPO.OrderNumber}.";

        SelectedPO = null;
        GRNNotes = string.Empty;
        await ReloadAsync(ct);
    });

    // ── Confirm GRN (#366 / #367) ──

    [RelayCommand]
    private Task ConfirmGRNAsync() => RunAsync(async ct =>
    {
        if (SelectedGRN is null) return;

        if (!dialogService.Confirm(
            $"Confirm GRN {SelectedGRN.GRNNumber}?\n\nThis will update stock quantities and cost prices.",
            "Confirm GRN"))
            return;

        // Default: receive all expected quantities
        var lines = SelectedGRN.Items.Select(i => new GRNReceiveLine(
            i.Id, i.QtyExpected, 0)).ToList();

        await grnService.ConfirmAsync(SelectedGRN.Id, lines, ct);
        SuccessMessage = $"GRN {SelectedGRN.GRNNumber} confirmed. Stock updated.";
        await ReloadAsync(ct);
    });

    // ── Cancel GRN ──

    [RelayCommand]
    private Task CancelGRNAsync() => RunAsync(async ct =>
    {
        if (SelectedGRN is null) return;
        if (!dialogService.Confirm($"Cancel GRN {SelectedGRN.GRNNumber}?", "Cancel GRN"))
            return;

        await grnService.CancelAsync(SelectedGRN.Id, ct);
        SuccessMessage = $"GRN {SelectedGRN.GRNNumber} cancelled.";
        await ReloadAsync(ct);
    });

    // ── Export (#373) ──

    [RelayCommand]
    private void ExportGrnCsv()
    {
        if (GRNs.Count == 0) return;
        var rows = GRNs.Select(g => new
        {
            g.Id, g.GRNNumber, g.ReceivedDate,
            Supplier = g.Supplier?.Name,
            Status = g.Status.ToString(),
            g.TotalAmount, g.Notes
        });
        if (CsvExporter.Export(rows, "GoodsReceivedNotes.csv"))
            SuccessMessage = "GRN data exported.";
    }

    // ── Create direct GRN without PO (#364) ──

    [RelayCommand]
    private Task CreateDirectGRNAsync() => RunAsync(async ct =>
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        var enteredItems = DirectLineItems
            .Where(line => line.ProductId > 0 || line.QtyExpected > 0)
            .ToList();

        if (!Validate(v => v
            .Rule(SelectedSupplier is not null, "Select a supplier.")
            .Rule(enteredItems.Count > 0, "Add at least one item.")))
            return;

        var invalidLine = enteredItems
            .Select((line, index) => new { line, index })
            .FirstOrDefault(x => x.line.ProductId <= 0 || x.line.QtyExpected <= 0 || x.line.UnitCost <= 0);

        if (invalidLine is not null)
        {
            ErrorMessage = $"Complete line {invalidLine.index + 1} with product, quantity, and unit cost.";
            return;
        }

        var dto = new CreateGRNDto(
            SelectedSupplier!.Id,
            string.IsNullOrWhiteSpace(GRNNotes) ? null : GRNNotes.Trim(),
            enteredItems.Select(l => new GRNLineDto(l.ProductId, l.QtyExpected, l.UnitCost)).ToList());

        var grn = await grnService.CreateDirectAsync(dto, ct);
        SuccessMessage = $"GRN {grn.GRNNumber} created (direct receipt).";

        SelectedSupplier = null;
        GRNNotes = string.Empty;
        DirectLineItems.Clear();
        EnsureDirectSeedLine();

        await ReloadAsync(ct);
    });

    [RelayCommand]
    private void AddDirectLine()
    {
        DirectLineItems.Add(new GRNLineInput());
    }

    [RelayCommand]
    private void RemoveDirectLine(GRNLineInput? line)
    {
        if (line is not null && DirectLineItems.Count > 1)
            DirectLineItems.Remove(line);
    }

    private void EnsureDirectSeedLine()
    {
        if (DirectLineItems.Count == 0)
            DirectLineItems.Add(new GRNLineInput());
    }

    private void RestoreViewState()
    {
        _isRestoringViewState = true;
        try
        {
            var state = UserPreferencesStore.GetGoodsReceivedNotesState();
            SearchQuery = state.SearchText;
            CurrentPage = state.CurrentPage;
            FilterStatus = string.Equals(state.ActiveFilter, "All", StringComparison.OrdinalIgnoreCase)
                ? null
                : Enum.TryParse<GRNStatus>(state.ActiveFilter, true, out var status)
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

        UserPreferencesStore.SetGoodsReceivedNotesState(new PagedSearchFilterViewState
        {
            SearchText = SearchQuery,
            ActiveFilter = FilterStatus?.ToString() ?? "All",
            CurrentPage = CurrentPage
        });
    }
}
