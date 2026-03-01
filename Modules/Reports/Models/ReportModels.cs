namespace StoreAssistantPro.Modules.Reports.Models;

/// <summary>Day-wise sales summary row.</summary>
public sealed record DaySalesSummary(
    DateOnly Date,
    int BillCount,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal NetAmount);

/// <summary>Month-wise sales summary row.</summary>
public sealed record MonthSalesSummary(
    int Year,
    int Month,
    string MonthName,
    int BillCount,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal NetAmount);

/// <summary>Category-wise stock summary row.</summary>
public sealed record CategoryStockSummary(
    string CategoryName,
    int ProductCount,
    int TotalQuantity,
    decimal TotalCostValue,
    decimal TotalRetailValue);

/// <summary>Category-wise sales summary row.</summary>
public sealed record CategorySalesSummary(
    string CategoryName,
    int ItemsSold,
    decimal TotalAmount);

/// <summary>Profit/loss summary row.</summary>
public sealed record ProfitLossSummary(
    DateOnly Date,
    decimal TotalSales,
    decimal TotalCost,
    decimal GrossProfit,
    decimal DiscountsGiven,
    decimal NetProfit);

/// <summary>Staff incentive summary row.</summary>
public sealed record StaffIncentiveSummary(
    string StaffName,
    string? StaffCode,
    decimal TotalSales,
    decimal IncentivableSales,
    decimal NormalIncentive,
    decimal SpecialIncentive,
    decimal TotalIncentive);
