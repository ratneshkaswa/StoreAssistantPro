using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.Services;

[Collection("UserPreferences")]
public sealed class UserPreferencesStoreTests : IDisposable
{
    private readonly string _filePath = Path.Combine(Path.GetTempPath(), $"storeassistantpro-prefs-{Guid.NewGuid():N}.json");

    public UserPreferencesStoreTests()
    {
        UserPreferencesStore.OverrideFilePathForTests(_filePath);
        UserPreferencesStore.ClearForTests();
    }

    public void Dispose()
    {
        UserPreferencesStore.ClearForTests();
        UserPreferencesStore.OverrideFilePathForTests(null);
    }

    [Fact]
    public void Update_Should_Persist_And_Reload_Snapshot()
    {
        UserPreferencesStore.Update(state =>
        {
            state.IsCompactModeEnabled = true;
            state.IsNavigationRailExpanded = true;
            state.LastVisitedPage = "Reports";
            state.RecentCommandPaletteItemIds = ["Reports", "Billing"];
        });

        var snapshot = UserPreferencesStore.GetSnapshot();

        Assert.True(snapshot.IsCompactModeEnabled);
        Assert.True(snapshot.IsNavigationRailExpanded);
        Assert.Equal("Reports", snapshot.LastVisitedPage);
        Assert.Equal(["Reports", "Billing"], snapshot.RecentCommandPaletteItemIds);
    }

    [Fact]
    public void MeetsNotificationThreshold_Should_Respect_Minimum_Level()
    {
        UserPreferencesStore.Update(state => state.MinimumNotificationLevel = AppNotificationLevel.Warning);

        Assert.False(UserPreferencesStore.MeetsNotificationThreshold(AppNotificationLevel.Info));
        Assert.False(UserPreferencesStore.MeetsNotificationThreshold(AppNotificationLevel.Success));
        Assert.True(UserPreferencesStore.MeetsNotificationThreshold(AppNotificationLevel.Warning));
        Assert.True(UserPreferencesStore.MeetsNotificationThreshold(AppNotificationLevel.Error));
    }

    [Fact]
    public void PrintPreviewZoom_And_DataGridLayout_Should_RoundTrip()
    {
        var layout = new DataGridLayoutState
        {
            Columns =
            [
                new DataGridColumnLayoutState { Header = "Invoice", DisplayIndex = 0, IsVisible = true, Width = 160 },
                new DataGridColumnLayoutState { Header = "Total", DisplayIndex = 1, IsVisible = false, Width = 96 }
            ]
        };

        UserPreferencesStore.SetPrintPreviewZoom("Sales", 140);
        UserPreferencesStore.SetDataGridLayout("SaleHistoryGrid", layout);

        Assert.Equal(140, UserPreferencesStore.GetPrintPreviewZoom("Sales", 100));
        var restoredLayout = UserPreferencesStore.GetDataGridLayout("SaleHistoryGrid");
        Assert.NotNull(restoredLayout);
        Assert.Equal(2, restoredLayout!.Columns.Count);
        Assert.Equal("Total", restoredLayout.Columns[1].Header);
        Assert.False(restoredLayout.Columns[1].IsVisible);
    }

    [Fact]
    public void PageStates_Should_RoundTrip()
    {
        UserPreferencesStore.SetSaleHistoryState(new SaleHistoryViewState
        {
            DateFrom = new DateTime(2026, 3, 1),
            DateTo = new DateTime(2026, 3, 31),
            InvoiceSearch = "INV-42"
        });
        UserPreferencesStore.SetProductManagementState(new ProductManagementViewState
        {
            SearchText = "shirt",
            FilterCategoryId = 4,
            FilterBrandId = 7,
            FilterStockStatus = "Low Stock"
        });
        UserPreferencesStore.SetReportsState(new ReportsViewState
        {
            DateFrom = new DateTime(2026, 2, 1),
            DateTo = new DateTime(2026, 2, 28),
            ActivePreset = "Last Month"
        });
        UserPreferencesStore.SetBranchManagementState(new SearchFilterViewState
        {
            SearchText = "B-17",
            ActiveFilter = "Pending"
        });
        UserPreferencesStore.SetDebtorManagementState(new SearchFilterViewState
        {
            SearchText = "Asha",
            ActiveFilter = "Cleared"
        });
        UserPreferencesStore.SetPaymentManagementState(new SearchFilterViewState
        {
            SearchText = "cash",
            ActiveFilter = "Month"
        });
        UserPreferencesStore.SetExpenseManagementState(new PagedSearchFilterViewState
        {
            SearchText = "rent",
            ActiveFilter = "Week",
            CurrentPage = 3
        });
        UserPreferencesStore.SetOrderManagementState(new SearchFilterViewState
        {
            SearchText = "Sharma",
            ActiveFilter = "Active"
        });
        UserPreferencesStore.SetIroningManagementState(new SearchFilterViewState
        {
            SearchText = "steam",
            ActiveFilter = "Paid"
        });
        UserPreferencesStore.SetSalaryManagementState(new SearchFilterViewState
        {
            SearchText = "march",
            ActiveFilter = "Unpaid"
        });
        UserPreferencesStore.SetSalesPurchaseManagementState(new DualFilterViewState
        {
            SearchText = "bill",
            PrimaryFilter = "Month",
            SecondaryFilter = "Sales"
        });
        UserPreferencesStore.SetPurchaseOrdersState(new PagedSearchFilterViewState
        {
            SearchText = "PO-17",
            ActiveFilter = "Ordered",
            CurrentPage = 2
        });
        UserPreferencesStore.SetQuotationsState(new PagedSearchFilterViewState
        {
            SearchText = "QT-19",
            ActiveFilter = "Accepted",
            CurrentPage = 4
        });
        UserPreferencesStore.SetGoodsReceivedNotesState(new PagedSearchFilterViewState
        {
            SearchText = "GRN-21",
            ActiveFilter = "Confirmed",
            CurrentPage = 5
        });

        var saleHistory = UserPreferencesStore.GetSaleHistoryState();
        var productManagement = UserPreferencesStore.GetProductManagementState();
        var reports = UserPreferencesStore.GetReportsState();
        var branchManagement = UserPreferencesStore.GetBranchManagementState();
        var debtorManagement = UserPreferencesStore.GetDebtorManagementState();
        var paymentManagement = UserPreferencesStore.GetPaymentManagementState();
        var expenseManagement = UserPreferencesStore.GetExpenseManagementState();
        var orderManagement = UserPreferencesStore.GetOrderManagementState();
        var ironingManagement = UserPreferencesStore.GetIroningManagementState();
        var salaryManagement = UserPreferencesStore.GetSalaryManagementState();
        var salesPurchaseManagement = UserPreferencesStore.GetSalesPurchaseManagementState();
        var purchaseOrders = UserPreferencesStore.GetPurchaseOrdersState();
        var quotations = UserPreferencesStore.GetQuotationsState();
        var goodsReceivedNotes = UserPreferencesStore.GetGoodsReceivedNotesState();

        Assert.Equal(new DateTime(2026, 3, 1), saleHistory.DateFrom);
        Assert.Equal(new DateTime(2026, 3, 31), saleHistory.DateTo);
        Assert.Equal("INV-42", saleHistory.InvoiceSearch);

        Assert.Equal("shirt", productManagement.SearchText);
        Assert.Equal(4, productManagement.FilterCategoryId);
        Assert.Equal(7, productManagement.FilterBrandId);
        Assert.Equal("Low Stock", productManagement.FilterStockStatus);

        Assert.Equal(new DateTime(2026, 2, 1), reports.DateFrom);
        Assert.Equal(new DateTime(2026, 2, 28), reports.DateTo);
        Assert.Equal("Last Month", reports.ActivePreset);

        Assert.Equal("B-17", branchManagement.SearchText);
        Assert.Equal("Pending", branchManagement.ActiveFilter);

        Assert.Equal("Asha", debtorManagement.SearchText);
        Assert.Equal("Cleared", debtorManagement.ActiveFilter);

        Assert.Equal("cash", paymentManagement.SearchText);
        Assert.Equal("Month", paymentManagement.ActiveFilter);

        Assert.Equal("rent", expenseManagement.SearchText);
        Assert.Equal("Week", expenseManagement.ActiveFilter);
        Assert.Equal(3, expenseManagement.CurrentPage);

        Assert.Equal("Sharma", orderManagement.SearchText);
        Assert.Equal("Active", orderManagement.ActiveFilter);

        Assert.Equal("steam", ironingManagement.SearchText);
        Assert.Equal("Paid", ironingManagement.ActiveFilter);

        Assert.Equal("march", salaryManagement.SearchText);
        Assert.Equal("Unpaid", salaryManagement.ActiveFilter);

        Assert.Equal("bill", salesPurchaseManagement.SearchText);
        Assert.Equal("Month", salesPurchaseManagement.PrimaryFilter);
        Assert.Equal("Sales", salesPurchaseManagement.SecondaryFilter);

        Assert.Equal("PO-17", purchaseOrders.SearchText);
        Assert.Equal("Ordered", purchaseOrders.ActiveFilter);
        Assert.Equal(2, purchaseOrders.CurrentPage);

        Assert.Equal("QT-19", quotations.SearchText);
        Assert.Equal("Accepted", quotations.ActiveFilter);
        Assert.Equal(4, quotations.CurrentPage);

        Assert.Equal("GRN-21", goodsReceivedNotes.SearchText);
        Assert.Equal("Confirmed", goodsReceivedNotes.ActiveFilter);
        Assert.Equal(5, goodsReceivedNotes.CurrentPage);
    }
}
