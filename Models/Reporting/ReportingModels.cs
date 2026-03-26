namespace StoreAssistantPro.Models.Reporting;

/// <summary>Custom report definition (#940).</summary>
public sealed class CustomReport
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DataSource { get; set; } = string.Empty; // Sales, Products, Customers, Expenses
    public string? ColumnsJson { get; set; }
    public string? FiltersJson { get; set; }
    public string? SortJson { get; set; }
    public string? ChartType { get; set; }
    public int CreatedByUserId { get; set; }
    public bool IsBookmarked { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Report schedule (#941).</summary>
public sealed class ReportSchedule
{
    public int Id { get; set; }
    public int CustomReportId { get; set; }
    public string Frequency { get; set; } = "Weekly"; // Daily, Weekly, Monthly
    public string? RecipientEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSentAt { get; set; }
    public DateTime? NextRunAt { get; set; }
}

/// <summary>KPI metric definition (#946).</summary>
public sealed record KpiMetric(
    string Name,
    string Category,
    decimal Value,
    decimal? PreviousValue,
    string? Unit,
    string? Trend);

/// <summary>Comparative report entry (#943).</summary>
public sealed record ComparativeEntry(
    string Label,
    decimal CurrentPeriod,
    decimal PreviousPeriod,
    decimal Change,
    double ChangePercent);
