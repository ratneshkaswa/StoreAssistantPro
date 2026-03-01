using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Modules.Reports.Models;

namespace StoreAssistantPro.Modules.Reports.Services;

public class ReportingService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional) : IReportingService
{
    public async Task<List<DaySalesSummary>> GetDayWiseSalesAsync(DateTime from, DateTime to)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Sales.AsNoTracking()
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new DaySalesSummary(
                DateOnly.FromDateTime(g.Key),
                g.Count(),
                g.Sum(s => s.TotalAmount),
                g.Sum(s => s.DiscountAmount),
                g.Sum(s => s.TotalAmount) - g.Sum(s => s.DiscountAmount)))
            .OrderBy(d => d.Date)
            .ToListAsync();
    }

    public async Task<List<MonthSalesSummary>> GetMonthWiseSalesAsync(DateTime from, DateTime to)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var raw = await db.Sales.AsNoTracking()
            .Where(s => s.SaleDate >= from && s.SaleDate <= to)
            .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                BillCount = g.Count(),
                TotalAmount = g.Sum(s => s.TotalAmount),
                DiscountAmount = g.Sum(s => s.DiscountAmount)
            })
            .OrderBy(g => g.Year).ThenBy(g => g.Month)
            .ToListAsync();

        return raw.Select(r => new MonthSalesSummary(
            r.Year, r.Month,
            CultureInfo.GetCultureInfo("en-IN").DateTimeFormat.GetMonthName(r.Month),
            r.BillCount, r.TotalAmount, r.DiscountAmount,
            r.TotalAmount - r.DiscountAmount)).ToList();
    }

    public async Task<List<CategoryStockSummary>> GetCategoryWiseStockAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .GroupBy(p => p.CategoryId.HasValue
                ? db.Categories.Where(c => c.Id == p.CategoryId.Value).Select(c => c.Name).FirstOrDefault() ?? "Uncategorized"
                : "Uncategorized")
            .Select(g => new CategoryStockSummary(
                g.Key,
                g.Count(),
                g.Sum(p => p.Quantity),
                g.Sum(p => p.CostPrice * p.Quantity),
                g.Sum(p => p.SalePrice * p.Quantity)))
            .OrderBy(c => c.CategoryName)
            .ToListAsync();
    }

    public async Task<List<CategorySalesSummary>> GetCategoryWiseSalesAsync(DateTime from, DateTime to)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        return await db.SaleItems.AsNoTracking()
            .Where(si => si.Sale!.SaleDate >= from && si.Sale.SaleDate <= to)
            .GroupBy(si => si.Product!.CategoryId.HasValue
                ? db.Categories.Where(c => c.Id == si.Product.CategoryId.Value).Select(c => c.Name).FirstOrDefault() ?? "Uncategorized"
                : "Uncategorized")
            .Select(g => new CategorySalesSummary(
                g.Key,
                g.Sum(si => si.Quantity),
                g.Sum(si => si.Quantity * si.UnitPrice)))
            .OrderByDescending(c => c.TotalAmount)
            .ToListAsync();
    }

    public async Task<List<ProfitLossSummary>> GetProfitLossAsync(DateTime from, DateTime to)
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var raw = await db.SaleItems.AsNoTracking()
            .Where(si => si.Sale!.SaleDate >= from && si.Sale.SaleDate <= to)
            .Include(si => si.Product)
            .Include(si => si.Sale)
            .GroupBy(si => si.Sale!.SaleDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                TotalSales = g.Sum(si => si.Quantity * si.UnitPrice),
                TotalCost = g.Sum(si => si.Quantity * (si.Product != null ? si.Product.CostPrice : 0m)),
                DiscountsGiven = g.Select(si => si.Sale!).Distinct().Sum(s => s.DiscountAmount)
            })
            .OrderBy(r => r.Date)
            .ToListAsync();

        return raw.Select(r => new ProfitLossSummary(
            DateOnly.FromDateTime(r.Date),
            r.TotalSales,
            r.TotalCost,
            r.TotalSales - r.TotalCost,
            r.DiscountsGiven,
            r.TotalSales - r.TotalCost - r.DiscountsGiven)).ToList();
    }

    public async Task<decimal> GetTodaysSalesAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var today = regional.Now.Date;
        return await db.Sales.AsNoTracking()
            .Where(s => s.SaleDate >= today && s.SaleDate < today.AddDays(1))
            .SumAsync(s => s.TotalAmount - s.DiscountAmount);
    }

    public async Task<int> GetTodaysBillCountAsync()
    {
        await using var db = await contextFactory.CreateDbContextAsync();
        var today = regional.Now.Date;
        return await db.Sales.AsNoTracking()
            .Where(s => s.SaleDate >= today && s.SaleDate < today.AddDays(1))
            .CountAsync();
    }
}
