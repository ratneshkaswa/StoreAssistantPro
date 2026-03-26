using Microsoft.Extensions.Logging;
using StoreAssistantPro.Models.UIPolish;

namespace StoreAssistantPro.Modules.UIPolish.Services;

public sealed class AnimationService(ILogger<AnimationService> logger) : IAnimationService
{
    public bool AreAnimationsEnabled { get; } = true;

    public string GetPageTransitionType(string? fromPage, string toPage)
    {
        logger.LogDebug("Page transition: {From} → {To}", fromPage, toPage);
        return "SlideFade";
    }

    public string GetFormRevealAnimation(string formType) => "SlideUpFadeIn";
    public string GetDialogOpenAnimation() => "DialogScaleIn";
    public string GetDialogCloseAnimation() => "DialogFadeOut";
}

public sealed class SkeletonService : ISkeletonService
{
    private readonly Dictionary<string, bool> _loadingStates = [];

    private static readonly Dictionary<string, SkeletonConfig> Configs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ProductsView"] = new() { RowCount = 10, ColumnCount = 6, RowHeight = 44 },
        ["SalesView"] = new() { RowCount = 8, ColumnCount = 5, RowHeight = 44 },
        ["CustomersView"] = new() { RowCount = 8, ColumnCount = 4, RowHeight = 44 },
        ["DashboardView"] = new() { RowCount = 4, ColumnCount = 3, RowHeight = 80, AnimationType = "Shimmer" },
        ["ReportsView"] = new() { RowCount = 6, ColumnCount = 4, RowHeight = 48 }
    };

    public SkeletonConfig GetConfigForView(string viewName)
        => Configs.TryGetValue(viewName, out var config) ? config : new SkeletonConfig();

    public bool IsLoading(string viewName) => _loadingStates.TryGetValue(viewName, out var v) && v;
    public void SetLoading(string viewName, bool isLoading) => _loadingStates[viewName] = isLoading;
}

public sealed class ProgressService : IProgressService
{
    public ProgressState? CurrentProgress { get; private set; }
    public event EventHandler<ProgressState>? ProgressChanged;

    public IDisposable BeginOperation(string operationName, int totalSteps, bool isCancellable = false)
    {
        CurrentProgress = new ProgressState
        {
            OperationName = operationName,
            Total = totalSteps,
            IsCancellable = isCancellable,
            IsIndeterminate = totalSteps <= 0
        };
        ProgressChanged?.Invoke(this, CurrentProgress);
        return new ProgressScope(this);
    }

    public void Report(int current, string? statusMessage = null)
    {
        if (CurrentProgress is null) return;
        CurrentProgress.Current = current;
        CurrentProgress.StatusMessage = statusMessage;
        ProgressChanged?.Invoke(this, CurrentProgress);
    }

    public void Complete(string? message = null)
    {
        if (CurrentProgress is not null)
        {
            CurrentProgress.Current = CurrentProgress.Total;
            CurrentProgress.StatusMessage = message ?? "Completed";
            ProgressChanged?.Invoke(this, CurrentProgress);
        }
        CurrentProgress = null;
    }

    public void Fail(string error)
    {
        if (CurrentProgress is not null)
        {
            CurrentProgress.StatusMessage = error;
            ProgressChanged?.Invoke(this, CurrentProgress);
        }
        CurrentProgress = null;
    }

    private sealed class ProgressScope(ProgressService svc) : IDisposable
    {
        public void Dispose() => svc.CurrentProgress = null;
    }
}

public sealed class IconService : IIconService
{
    public string FontFamily => "Segoe Fluent Icons";

    private static readonly Dictionary<string, string> Icons = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Add"] = "\uE710", ["Delete"] = "\uE74D", ["Edit"] = "\uE70F",
        ["Save"] = "\uE74E", ["Search"] = "\uE721", ["Filter"] = "\uE71C",
        ["Settings"] = "\uE713", ["Print"] = "\uE749", ["Export"] = "\uE78B",
        ["Import"] = "\uE8B5", ["Refresh"] = "\uE72C", ["Close"] = "\uE711",
        ["Check"] = "\uE73E", ["Warning"] = "\uE7BA", ["Error"] = "\uE783",
        ["Info"] = "\uE946", ["Home"] = "\uE80F", ["Cart"] = "\uE7BF",
        ["User"] = "\uE77B", ["Store"] = "\uE7BF", ["Chart"] = "\uE9D9",
        ["Calendar"] = "\uE787", ["Clock"] = "\uE823", ["Money"] = "\uE8C7",
        ["Barcode"] = "\uE8C8", ["Camera"] = "\uE722", ["Notification"] = "\uEA8F"
    };

    public string GetGlyph(string iconName)
        => Icons.TryGetValue(iconName, out var glyph) ? glyph : "\uE739";

    public IReadOnlyList<string> GetAvailableIcons() => Icons.Keys.ToList();
}

public sealed class ResponsiveLayoutService : IResponsiveLayoutService
{
    public double CurrentDpi { get; } = 96;
    public double ScaleFactor => CurrentDpi / 96.0;
    public bool IsHighDpi => CurrentDpi > 96;
    public (double Width, double Height) ScreenSize => (1920, 1080);
    public event EventHandler? DpiChanged;

    internal void OnDpiChanged() => DpiChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class StatusBadgeService : IStatusBadgeService
{
    public StatusBadgeType GetBadgeType(string status) => status.ToUpperInvariant() switch
    {
        "ACTIVE" or "IN STOCK" => StatusBadgeType.Active,
        "INACTIVE" or "DISABLED" => StatusBadgeType.Inactive,
        "LOW STOCK" or "WARNING" => StatusBadgeType.LowStock,
        "OUT OF STOCK" or "OUT" => StatusBadgeType.OutOfStock,
        "ERROR" or "FAILED" => StatusBadgeType.Error,
        _ => StatusBadgeType.Info
    };

    public string GetBadgeColor(StatusBadgeType badgeType) => badgeType switch
    {
        StatusBadgeType.Active => "#0F7B0F",
        StatusBadgeType.Inactive => "#8A8886",
        StatusBadgeType.Warning or StatusBadgeType.LowStock => "#CA5010",
        StatusBadgeType.Error or StatusBadgeType.OutOfStock => "#C42B1C",
        StatusBadgeType.Info => "#0078D4",
        _ => "#323130"
    };

    public string GetBadgeLabel(StatusBadgeType badgeType) => badgeType switch
    {
        StatusBadgeType.Active => "Active",
        StatusBadgeType.Inactive => "Inactive",
        StatusBadgeType.Warning => "Warning",
        StatusBadgeType.LowStock => "Low Stock",
        StatusBadgeType.OutOfStock => "Out of Stock",
        StatusBadgeType.Error => "Error",
        StatusBadgeType.Info => "Info",
        _ => "Unknown"
    };
}

public sealed class ChartService : IChartService
{
    private static readonly string[] DefaultColors = ["#0078D4", "#00B7C3", "#498205", "#CA5010", "#8764B8", "#E3008C"];

    public ChartConfig CreateBarChart(string title, IReadOnlyList<ChartDataPoint> data) => new()
    {
        ChartType = "Bar", Title = title, DataPoints = AssignColors(data)
    };

    public ChartConfig CreateLineChart(string title, IReadOnlyList<ChartDataPoint> data) => new()
    {
        ChartType = "Line", Title = title, DataPoints = AssignColors(data)
    };

    public ChartConfig CreatePieChart(string title, IReadOnlyList<ChartDataPoint> data) => new()
    {
        ChartType = "Pie", Title = title, DataPoints = AssignColors(data), ShowLegend = true
    };

    private static IReadOnlyList<ChartDataPoint> AssignColors(IReadOnlyList<ChartDataPoint> data)
        => data.Select((d, i) => d with { Color = d.Color ?? DefaultColors[i % DefaultColors.Length] }).ToList();
}
