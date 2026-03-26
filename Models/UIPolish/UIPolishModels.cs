namespace StoreAssistantPro.Models.UIPolish;

/// <summary>
/// Skeleton loading placeholder configuration (#852).
/// </summary>
public sealed class SkeletonConfig
{
    public int RowCount { get; set; } = 5;
    public int ColumnCount { get; set; } = 4;
    public bool ShowHeader { get; set; } = true;
    public double RowHeight { get; set; } = 40;
    public string AnimationType { get; set; } = "Pulse"; // Pulse, Wave, Shimmer
}

/// <summary>
/// Progress indicator state for long operations (#853).
/// </summary>
public sealed class ProgressState
{
    public string OperationName { get; set; } = string.Empty;
    public int Current { get; set; }
    public int Total { get; set; }
    public double PercentComplete => Total > 0 ? (double)Current / Total * 100 : 0;
    public bool IsIndeterminate { get; set; }
    public string? StatusMessage { get; set; }
    public bool IsCancellable { get; set; }
}

/// <summary>
/// Empty state configuration for views (#864).
/// </summary>
public sealed class EmptyStateConfig
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconGlyph { get; set; }
    public string? ActionLabel { get; set; }
    public string? ActionCommandName { get; set; }
}

/// <summary>
/// Status badge type for color-coded badges (#865).
/// </summary>
public enum StatusBadgeType
{
    Active,
    Inactive,
    Warning,
    Error,
    Info,
    LowStock,
    OutOfStock
}

/// <summary>
/// Chart data point for data visualization (#866).
/// </summary>
public sealed record ChartDataPoint(
    string Label,
    double Value,
    string? Category = null,
    string? Color = null);

/// <summary>
/// Chart configuration for data visualization.
/// </summary>
public sealed class ChartConfig
{
    public string ChartType { get; set; } = "Bar"; // Bar, Line, Pie, Doughnut
    public string Title { get; set; } = string.Empty;
    public IReadOnlyList<ChartDataPoint> DataPoints { get; set; } = [];
    public bool ShowLegend { get; set; } = true;
    public bool ShowLabels { get; set; } = true;
    public string? XAxisLabel { get; set; }
    public string? YAxisLabel { get; set; }
}
