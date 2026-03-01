using StoreAssistantPro.Modules.Reports.Models;

namespace StoreAssistantPro.Modules.Reports.Services;

public interface IReportingService
{
    Task<List<DaySalesSummary>> GetDayWiseSalesAsync(DateTime from, DateTime to);
    Task<List<MonthSalesSummary>> GetMonthWiseSalesAsync(DateTime from, DateTime to);
    Task<List<CategoryStockSummary>> GetCategoryWiseStockAsync();
    Task<List<CategorySalesSummary>> GetCategoryWiseSalesAsync(DateTime from, DateTime to);
    Task<List<ProfitLossSummary>> GetProfitLossAsync(DateTime from, DateTime to);
    Task<decimal> GetTodaysSalesAsync();
    Task<int> GetTodaysBillCountAsync();
}
