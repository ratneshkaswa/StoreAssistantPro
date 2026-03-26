using System.IO;
using System.Text.Json;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

public sealed class UserPreferencesState
{
    public bool IsCompactModeEnabled { get; set; }

    public bool IsNavigationRailExpanded { get; set; }

    public bool RestoreLastVisitedPageOnLogin { get; set; } = true;

    public string? LastVisitedPage { get; set; }

    public bool InAppToastsEnabled { get; set; } = true;

    public bool WindowsNotificationsEnabled { get; set; } = true;

    public bool NotificationSoundEnabled { get; set; }

    public AppNotificationLevel MinimumNotificationLevel { get; set; } = AppNotificationLevel.Info;

    public List<string> RecentCommandPaletteItemIds { get; set; } = [];

    public Dictionary<string, double> PrintPreviewZoomLevels { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, DataGridLayoutState> DataGridLayouts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public SaleHistoryViewState SaleHistory { get; set; } = new();

    public ProductManagementViewState ProductManagement { get; set; } = new();

    public ReportsViewState Reports { get; set; } = new();

    public SearchFilterViewState BranchManagement { get; set; } = new();

    public SearchFilterViewState DebtorManagement { get; set; } = new();

    public SearchFilterViewState PaymentManagement { get; set; } = new();

    public PagedSearchFilterViewState ExpenseManagement { get; set; } = new();

    public SearchFilterViewState OrderManagement { get; set; } = new();

    public SearchFilterViewState IroningManagement { get; set; } = new();

    public SearchFilterViewState SalaryManagement { get; set; } = new();

    public DualFilterViewState SalesPurchaseManagement { get; set; } = new();

    public PagedSearchFilterViewState PurchaseOrders { get; set; } = new();

    public PagedSearchFilterViewState Quotations { get; set; } = new();

    public PagedSearchFilterViewState GoodsReceivedNotes { get; set; } = new();
}

public sealed class DataGridLayoutState
{
    public List<DataGridColumnLayoutState> Columns { get; set; } = [];
}

public sealed class DataGridColumnLayoutState
{
    public string Header { get; set; } = string.Empty;

    public int DisplayIndex { get; set; }

    public bool IsVisible { get; set; } = true;

    public double? Width { get; set; }
}

public sealed class SaleHistoryViewState
{
    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public string InvoiceSearch { get; set; } = string.Empty;
}

public sealed class ProductManagementViewState
{
    public string SearchText { get; set; } = string.Empty;

    public int? FilterCategoryId { get; set; }

    public int? FilterBrandId { get; set; }

    public string FilterStockStatus { get; set; } = "All";
}

public sealed class ReportsViewState
{
    public DateTime DateFrom { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public DateTime DateTo { get; set; } = DateTime.Today;

    public string ActivePreset { get; set; } = "This Month";
}

public class SearchFilterViewState
{
    public string SearchText { get; set; } = string.Empty;

    public string ActiveFilter { get; set; } = "All";
}

public sealed class PagedSearchFilterViewState : SearchFilterViewState
{
    public int CurrentPage { get; set; } = 1;
}

public sealed class DualFilterViewState
{
    public string SearchText { get; set; } = string.Empty;

    public string PrimaryFilter { get; set; } = "All";

    public string SecondaryFilter { get; set; } = "All";
}

public static class UserPreferencesStore
{
    private static readonly Lock SyncRoot = new();
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private static UserPreferencesState? _cached;
    private static string? _overrideFilePath;

    public static UserPreferencesState GetSnapshot()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore());
        }
    }

    public static void Update(Action<UserPreferencesState> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        lock (SyncRoot)
        {
            var state = LoadCore();
            update(state);
            Normalize(state);
            SaveCore(state);
        }
    }

    public static bool MeetsNotificationThreshold(AppNotificationLevel level)
    {
        lock (SyncRoot)
        {
            return GetNotificationRank(level) >= GetNotificationRank(LoadCore().MinimumNotificationLevel);
        }
    }

    public static double GetPrintPreviewZoom(string? key, double fallback)
    {
        var normalizedKey = NormalizeKey(key, "PrintPreview");

        lock (SyncRoot)
        {
            return LoadCore().PrintPreviewZoomLevels.TryGetValue(normalizedKey, out var zoom)
                ? zoom
                : fallback;
        }
    }

    public static void SetPrintPreviewZoom(string? key, double zoom)
    {
        var normalizedKey = NormalizeKey(key, "PrintPreview");

        Update(state => state.PrintPreviewZoomLevels[normalizedKey] = zoom);
    }

    public static DataGridLayoutState? GetDataGridLayout(string? key)
    {
        var normalizedKey = NormalizeKey(key, string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedKey))
            return null;

        lock (SyncRoot)
        {
            return LoadCore().DataGridLayouts.TryGetValue(normalizedKey, out var layout)
                ? Clone(layout)
                : null;
        }
    }

    public static void SetDataGridLayout(string? key, DataGridLayoutState layout)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var normalizedKey = NormalizeKey(key, string.Empty);
        if (string.IsNullOrWhiteSpace(normalizedKey))
            return;

        Update(state => state.DataGridLayouts[normalizedKey] = Clone(layout));
    }

    public static SaleHistoryViewState GetSaleHistoryState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().SaleHistory);
        }
    }

    public static void SetSaleHistoryState(SaleHistoryViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.SaleHistory = Clone(state));
    }

    public static ProductManagementViewState GetProductManagementState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().ProductManagement);
        }
    }

    public static void SetProductManagementState(ProductManagementViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.ProductManagement = Clone(state));
    }

    public static ReportsViewState GetReportsState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().Reports);
        }
    }

    public static void SetReportsState(ReportsViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.Reports = Clone(state));
    }

    public static SearchFilterViewState GetBranchManagementState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().BranchManagement);
        }
    }

    public static void SetBranchManagementState(SearchFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.BranchManagement = Clone(state));
    }

    public static SearchFilterViewState GetDebtorManagementState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().DebtorManagement);
        }
    }

    public static void SetDebtorManagementState(SearchFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.DebtorManagement = Clone(state));
    }

    public static SearchFilterViewState GetPaymentManagementState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().PaymentManagement);
        }
    }

    public static void SetPaymentManagementState(SearchFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.PaymentManagement = Clone(state));
    }

    public static PagedSearchFilterViewState GetExpenseManagementState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().ExpenseManagement);
        }
    }

    public static void SetExpenseManagementState(PagedSearchFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.ExpenseManagement = Clone(state));
    }

    public static SearchFilterViewState GetOrderManagementState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().OrderManagement);
        }
    }

    public static void SetOrderManagementState(SearchFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.OrderManagement = Clone(state));
    }

    public static SearchFilterViewState GetIroningManagementState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().IroningManagement);
        }
    }

    public static void SetIroningManagementState(SearchFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.IroningManagement = Clone(state));
    }

    public static SearchFilterViewState GetSalaryManagementState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().SalaryManagement);
        }
    }

    public static void SetSalaryManagementState(SearchFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.SalaryManagement = Clone(state));
    }

    public static DualFilterViewState GetSalesPurchaseManagementState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().SalesPurchaseManagement);
        }
    }

    public static void SetSalesPurchaseManagementState(DualFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.SalesPurchaseManagement = Clone(state));
    }

    public static PagedSearchFilterViewState GetPurchaseOrdersState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().PurchaseOrders);
        }
    }

    public static void SetPurchaseOrdersState(PagedSearchFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.PurchaseOrders = Clone(state));
    }

    public static PagedSearchFilterViewState GetQuotationsState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().Quotations);
        }
    }

    public static void SetQuotationsState(PagedSearchFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.Quotations = Clone(state));
    }

    public static PagedSearchFilterViewState GetGoodsReceivedNotesState()
    {
        lock (SyncRoot)
        {
            return Clone(LoadCore().GoodsReceivedNotes);
        }
    }

    public static void SetGoodsReceivedNotesState(PagedSearchFilterViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Update(snapshot => snapshot.GoodsReceivedNotes = Clone(state));
    }

    internal static void OverrideFilePathForTests(string? filePath)
    {
        lock (SyncRoot)
        {
            _overrideFilePath = filePath;
            _cached = null;
        }
    }

    internal static void ClearForTests()
    {
        lock (SyncRoot)
        {
            var path = ResolveFilePath();
            _cached = null;
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static UserPreferencesState LoadCore()
    {
        if (_cached is not null)
            return _cached;

        var path = ResolveFilePath();
        if (!File.Exists(path))
        {
            _cached = CreateDefaultState();
            return _cached;
        }

        try
        {
            var json = File.ReadAllText(path);
            _cached = JsonSerializer.Deserialize<UserPreferencesState>(json, SerializerOptions) ?? CreateDefaultState();
        }
        catch
        {
            _cached = CreateDefaultState();
        }

        Normalize(_cached);
        return _cached;
    }

    private static void SaveCore(UserPreferencesState state)
    {
        var path = ResolveFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(state, SerializerOptions));
        _cached = Clone(state);
    }

    private static UserPreferencesState CreateDefaultState() => new()
    {
        IsCompactModeEnabled = false,
        IsNavigationRailExpanded = false,
        RestoreLastVisitedPageOnLogin = true,
        InAppToastsEnabled = true,
        WindowsNotificationsEnabled = true,
        NotificationSoundEnabled = false,
        MinimumNotificationLevel = AppNotificationLevel.Info
    };

    private static void Normalize(UserPreferencesState state)
    {
        state.LastVisitedPage = NormalizeKey(state.LastVisitedPage, string.Empty);

        state.RecentCommandPaletteItemIds = state.RecentCommandPaletteItemIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        state.PrintPreviewZoomLevels = state.PrintPreviewZoomLevels
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
            .ToDictionary(
                entry => entry.Key.Trim(),
                entry => entry.Value,
                StringComparer.OrdinalIgnoreCase);

        state.DataGridLayouts = state.DataGridLayouts
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
            .ToDictionary(
                entry => entry.Key.Trim(),
                entry => Clone(entry.Value),
                StringComparer.OrdinalIgnoreCase);

        state.SaleHistory = Clone(state.SaleHistory);
        state.SaleHistory.InvoiceSearch = state.SaleHistory.InvoiceSearch?.Trim() ?? string.Empty;

        state.ProductManagement = Clone(state.ProductManagement);
        state.ProductManagement.SearchText = state.ProductManagement.SearchText?.Trim() ?? string.Empty;
        state.ProductManagement.FilterStockStatus = NormalizeFilterStockStatus(state.ProductManagement.FilterStockStatus);

        state.Reports = Clone(state.Reports);
        if (state.Reports.DateTo < state.Reports.DateFrom)
            state.Reports.DateTo = state.Reports.DateFrom;
        state.Reports.ActivePreset = string.IsNullOrWhiteSpace(state.Reports.ActivePreset)
            ? "This Month"
            : state.Reports.ActivePreset.Trim();

        state.BranchManagement = NormalizeSearchFilterState(state.BranchManagement, NormalizeStatusFilter);
        state.DebtorManagement = NormalizeSearchFilterState(state.DebtorManagement, NormalizeStatusFilter);
        state.PaymentManagement = NormalizeSearchFilterState(state.PaymentManagement, NormalizeDateFilter);
        state.ExpenseManagement = NormalizePagedSearchFilterState(state.ExpenseManagement, NormalizeDateFilter);
        state.OrderManagement = NormalizeSearchFilterState(state.OrderManagement, NormalizeOrderStatusFilter);
        state.IroningManagement = NormalizeSearchFilterState(state.IroningManagement, NormalizePaidFilter);
        state.SalaryManagement = NormalizeSearchFilterState(state.SalaryManagement, NormalizePaidFilter);
        state.SalesPurchaseManagement = NormalizeDualFilterState(
            state.SalesPurchaseManagement,
            NormalizeDateFilter,
            NormalizeSalesPurchaseTypeFilter);
        state.PurchaseOrders = NormalizePagedSearchFilterState(state.PurchaseOrders, NormalizePurchaseOrderStatusFilter);
        state.Quotations = NormalizePagedSearchFilterState(state.Quotations, NormalizeQuotationStatusFilter);
        state.GoodsReceivedNotes = NormalizePagedSearchFilterState(state.GoodsReceivedNotes, NormalizeGrnStatusFilter);
    }

    private static string ResolveFilePath()
    {
        if (!string.IsNullOrWhiteSpace(_overrideFilePath))
            return _overrideFilePath;

        if (AppDomain.CurrentDomain.FriendlyName.Contains("testhost", StringComparison.OrdinalIgnoreCase) ||
            AppContext.BaseDirectory.Contains("StoreAssistantPro.Tests", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(AppContext.BaseDirectory, "user-preferences.test.json");
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StoreAssistantPro",
            "user-preferences.json");
    }

    private static string NormalizeKey(string? key, string fallback) =>
        string.IsNullOrWhiteSpace(key) ? fallback : key.Trim();

    private static int GetNotificationRank(AppNotificationLevel level) => level switch
    {
        AppNotificationLevel.Error => 3,
        AppNotificationLevel.Warning => 2,
        AppNotificationLevel.Success => 1,
        _ => 1
    };

    private static UserPreferencesState Clone(UserPreferencesState state) => new()
    {
        IsCompactModeEnabled = state.IsCompactModeEnabled,
        IsNavigationRailExpanded = state.IsNavigationRailExpanded,
        RestoreLastVisitedPageOnLogin = state.RestoreLastVisitedPageOnLogin,
        LastVisitedPage = state.LastVisitedPage,
        InAppToastsEnabled = state.InAppToastsEnabled,
        WindowsNotificationsEnabled = state.WindowsNotificationsEnabled,
        NotificationSoundEnabled = state.NotificationSoundEnabled,
        MinimumNotificationLevel = state.MinimumNotificationLevel,
        RecentCommandPaletteItemIds = [.. state.RecentCommandPaletteItemIds],
        PrintPreviewZoomLevels = new Dictionary<string, double>(state.PrintPreviewZoomLevels, StringComparer.OrdinalIgnoreCase),
        DataGridLayouts = state.DataGridLayouts.ToDictionary(
            entry => entry.Key,
            entry => Clone(entry.Value),
            StringComparer.OrdinalIgnoreCase),
        SaleHistory = Clone(state.SaleHistory),
        ProductManagement = Clone(state.ProductManagement),
        Reports = Clone(state.Reports),
        BranchManagement = Clone(state.BranchManagement),
        DebtorManagement = Clone(state.DebtorManagement),
        PaymentManagement = Clone(state.PaymentManagement),
        ExpenseManagement = Clone(state.ExpenseManagement),
        OrderManagement = Clone(state.OrderManagement),
        IroningManagement = Clone(state.IroningManagement),
        SalaryManagement = Clone(state.SalaryManagement),
        SalesPurchaseManagement = Clone(state.SalesPurchaseManagement),
        PurchaseOrders = Clone(state.PurchaseOrders),
        Quotations = Clone(state.Quotations),
        GoodsReceivedNotes = Clone(state.GoodsReceivedNotes)
    };

    private static DataGridLayoutState Clone(DataGridLayoutState layout) => new()
    {
        Columns =
        [
            .. layout.Columns.Select(column => new DataGridColumnLayoutState
            {
                Header = column.Header,
                DisplayIndex = column.DisplayIndex,
                IsVisible = column.IsVisible,
                Width = column.Width
            })
        ]
    };

    private static SaleHistoryViewState Clone(SaleHistoryViewState? state) => new()
    {
        DateFrom = state?.DateFrom,
        DateTo = state?.DateTo,
        InvoiceSearch = state?.InvoiceSearch ?? string.Empty
    };

    private static ProductManagementViewState Clone(ProductManagementViewState? state) => new()
    {
        SearchText = state?.SearchText ?? string.Empty,
        FilterCategoryId = state?.FilterCategoryId,
        FilterBrandId = state?.FilterBrandId,
        FilterStockStatus = NormalizeFilterStockStatus(state?.FilterStockStatus)
    };

    private static ReportsViewState Clone(ReportsViewState? state) => new()
    {
        DateFrom = state?.DateFrom ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
        DateTo = state?.DateTo ?? DateTime.Today,
        ActivePreset = string.IsNullOrWhiteSpace(state?.ActivePreset) ? "This Month" : state.ActivePreset.Trim()
    };

    private static SearchFilterViewState Clone(SearchFilterViewState? state) => new()
    {
        SearchText = state?.SearchText ?? string.Empty,
        ActiveFilter = string.IsNullOrWhiteSpace(state?.ActiveFilter) ? "All" : state.ActiveFilter.Trim()
    };

    private static PagedSearchFilterViewState Clone(PagedSearchFilterViewState? state) => new()
    {
        SearchText = state?.SearchText ?? string.Empty,
        ActiveFilter = string.IsNullOrWhiteSpace(state?.ActiveFilter) ? "All" : state.ActiveFilter.Trim(),
        CurrentPage = state?.CurrentPage > 0 ? state.CurrentPage : 1
    };

    private static DualFilterViewState Clone(DualFilterViewState? state) => new()
    {
        SearchText = state?.SearchText ?? string.Empty,
        PrimaryFilter = string.IsNullOrWhiteSpace(state?.PrimaryFilter) ? "All" : state.PrimaryFilter.Trim(),
        SecondaryFilter = string.IsNullOrWhiteSpace(state?.SecondaryFilter) ? "All" : state.SecondaryFilter.Trim()
    };

    private static string NormalizeFilterStockStatus(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();
        return normalized is "All" or "In Stock" or "Low Stock" or "Out of Stock"
            ? normalized
            : "All";
    }

    private static SearchFilterViewState NormalizeSearchFilterState(SearchFilterViewState? state, Func<string?, string> normalizeFilter) => new()
    {
        SearchText = state?.SearchText?.Trim() ?? string.Empty,
        ActiveFilter = normalizeFilter(state?.ActiveFilter)
    };

    private static PagedSearchFilterViewState NormalizePagedSearchFilterState(PagedSearchFilterViewState? state, Func<string?, string> normalizeFilter) => new()
    {
        SearchText = state?.SearchText?.Trim() ?? string.Empty,
        ActiveFilter = normalizeFilter(state?.ActiveFilter),
        CurrentPage = state?.CurrentPage > 0 ? state.CurrentPage : 1
    };

    private static DualFilterViewState NormalizeDualFilterState(
        DualFilterViewState? state,
        Func<string?, string> normalizePrimaryFilter,
        Func<string?, string> normalizeSecondaryFilter) => new()
    {
        SearchText = state?.SearchText?.Trim() ?? string.Empty,
        PrimaryFilter = normalizePrimaryFilter(state?.PrimaryFilter),
        SecondaryFilter = normalizeSecondaryFilter(state?.SecondaryFilter)
    };

    private static string NormalizeStatusFilter(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();
        return normalized is "All" or "Pending" or "Cleared" ? normalized : "All";
    }

    private static string NormalizeDateFilter(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();
        return normalized is "All" or "Today" or "Week" or "Month" ? normalized : "All";
    }

    private static string NormalizePaidFilter(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();
        return normalized is "All" or "Paid" or "Unpaid" ? normalized : "All";
    }

    private static string NormalizeOrderStatusFilter(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();
        return normalized is "All" or "Pending" or "Active" or "Delivered" ? normalized : "All";
    }

    private static string NormalizeSalesPurchaseTypeFilter(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();
        return normalized is "All" or "Sales" or "Purchase" ? normalized : "All";
    }

    private static string NormalizePurchaseOrderStatusFilter(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();
        return normalized is "All" or "Draft" or "Ordered" or "PartialReceived" or "Received" or "Cancelled"
            ? normalized
            : "All";
    }

    private static string NormalizeQuotationStatusFilter(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();
        return normalized is "All" or "Draft" or "Sent" or "Accepted" or "Rejected" or "Expired" or "ConvertedToSale"
            ? normalized
            : "All";
    }

    private static string NormalizeGrnStatusFilter(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "All" : value.Trim();
        return normalized is "All" or "Draft" or "Confirmed" or "Cancelled"
            ? normalized
            : "All";
    }
}
