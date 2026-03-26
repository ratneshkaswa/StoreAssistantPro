using StoreAssistantPro.Models.UIPolish;

namespace StoreAssistantPro.Modules.UIPolish.Services;

/// <summary>
/// Page and dialog animation service (#849, #850, #857).
/// Coordinates page transitions, form reveals, and dialog open/close animations.
/// </summary>
public interface IAnimationService
{
    /// <summary>Get page transition type for navigation (#849).</summary>
    string GetPageTransitionType(string? fromPage, string toPage);

    /// <summary>Get form reveal animation name (#850).</summary>
    string GetFormRevealAnimation(string formType);

    /// <summary>Get dialog open animation storyboard key (#857).</summary>
    string GetDialogOpenAnimation();

    /// <summary>Get dialog close animation storyboard key (#857).</summary>
    string GetDialogCloseAnimation();

    /// <summary>Whether animations are enabled (respects user preference and OS settings).</summary>
    bool AreAnimationsEnabled { get; }
}

/// <summary>
/// Loading skeleton service (#852).
/// Provides skeleton placeholder configurations for data loading states.
/// </summary>
public interface ISkeletonService
{
    SkeletonConfig GetConfigForView(string viewName);
    bool IsLoading(string viewName);
    void SetLoading(string viewName, bool isLoading);
}

/// <summary>
/// Progress tracking service for long operations (#853).
/// </summary>
public interface IProgressService
{
    ProgressState? CurrentProgress { get; }
    event EventHandler<ProgressState>? ProgressChanged;
    IDisposable BeginOperation(string operationName, int totalSteps, bool isCancellable = false);
    void Report(int current, string? statusMessage = null);
    void Complete(string? message = null);
    void Fail(string error);
}

/// <summary>
/// Icon library service (#859) for consistent icon lookup.
/// </summary>
public interface IIconService
{
    /// <summary>Get icon glyph character for a named icon.</summary>
    string GetGlyph(string iconName);

    /// <summary>Get icon font family name.</summary>
    string FontFamily { get; }

    /// <summary>Get all available icon names.</summary>
    IReadOnlyList<string> GetAvailableIcons();
}

/// <summary>
/// Responsive layout service (#861, #862).
/// Detects DPI, screen size, and adjusts layout accordingly.
/// </summary>
public interface IResponsiveLayoutService
{
    double CurrentDpi { get; }
    double ScaleFactor { get; }
    bool IsHighDpi { get; }
    (double Width, double Height) ScreenSize { get; }
    event EventHandler? DpiChanged;
}

/// <summary>
/// Status badge service for color-coded status indicators (#865).
/// </summary>
public interface IStatusBadgeService
{
    StatusBadgeType GetBadgeType(string status);
    string GetBadgeColor(StatusBadgeType badgeType);
    string GetBadgeLabel(StatusBadgeType badgeType);
}

/// <summary>
/// Data visualization chart service (#866).
/// Generates chart configurations for reports and dashboards.
/// </summary>
public interface IChartService
{
    ChartConfig CreateBarChart(string title, IReadOnlyList<ChartDataPoint> data);
    ChartConfig CreateLineChart(string title, IReadOnlyList<ChartDataPoint> data);
    ChartConfig CreatePieChart(string title, IReadOnlyList<ChartDataPoint> data);
}
