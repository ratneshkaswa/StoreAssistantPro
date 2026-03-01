using Microsoft.EntityFrameworkCore;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Data;
using StoreAssistantPro.Modules.Reports.Models;

namespace StoreAssistantPro.Modules.Staff.Services;

public class IncentiveService(
    IDbContextFactory<AppDbContext> contextFactory,
    IRegionalSettingsService regional) : IIncentiveService
{
    public async Task<List<StaffIncentiveSummary>> GetStaffIncentivesAsync(DateTime from, DateTime to)
    {
        await using var db = await contextFactory.CreateDbContextAsync();

        var staffSales = await db.SaleItems.AsNoTracking()
            .Where(si => si.StaffId != null
                         && si.Sale!.SaleDate >= from
                         && si.Sale.SaleDate <= to)
            .GroupBy(si => si.StaffId!.Value)
            .Select(g => new
            {
                StaffId = g.Key,
                TotalSales = g.Sum(si => si.Quantity * si.UnitPrice)
            })
            .ToListAsync();

        var staffList = await db.Staffs.AsNoTracking()
            .Where(s => s.IsActive)
            .ToListAsync();

        return staffList.Select(staff =>
        {
            var sales = staffSales.FirstOrDefault(s => s.StaffId == staff.Id);
            var totalSales = sales?.TotalSales ?? 0m;
            var normalIncentive = totalSales * staff.NormalIncentiveRate / 100m;
            var specialIncentive = totalSales * staff.SpecialIncentiveRate / 100m;

            return new StaffIncentiveSummary(
                staff.Name,
                staff.StaffCode,
                totalSales,
                totalSales,
                Math.Round(normalIncentive, 2),
                Math.Round(specialIncentive, 2),
                Math.Round(normalIncentive + specialIncentive, 2));
        })
        .Where(s => s.TotalSales > 0)
        .OrderByDescending(s => s.TotalSales)
        .ToList();
    }

    public async Task<StaffIncentiveSummary?> GetStaffIncentiveAsync(int staffId, DateTime from, DateTime to)
    {
        var all = await GetStaffIncentivesAsync(from, to);
        return all.FirstOrDefault(s => s.StaffCode != null);
    }

    public async Task<List<StaffIncentiveSummary>> GetTodaysIncentivesAsync()
    {
        var today = regional.Now.Date;
        return await GetStaffIncentivesAsync(today, today.AddDays(1));
    }
}
