namespace StoreAssistantPro.Core.Data;

/// <summary>
/// Describes a single page request. Passed to paged service methods
/// to control offset, page size, and optional server-side text search.
/// <para>
/// <b>Architecture rule:</b> Any list that may grow beyond ~100 rows
/// should use <see cref="PagedQuery"/> / <see cref="PagedResult{T}"/>
/// instead of unbounded <c>GetAllAsync</c>.
/// </para>
/// </summary>
public sealed record PagedQuery(
    int PageIndex = 0,
    int PageSize = 50,
    string? SearchTerm = null,
    StockFilter StockFilter = StockFilter.All,
    ActiveFilter ActiveFilter = ActiveFilter.All,
    int? BrandId = null,
    string? SortColumn = null,
    bool SortDescending = false,
    string? ColorFilter = null,
    int? TaxProfileId = null,
    string? UomFilter = null);
