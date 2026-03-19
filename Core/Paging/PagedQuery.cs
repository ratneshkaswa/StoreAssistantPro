namespace StoreAssistantPro.Core.Paging;

/// <summary>
/// Standard server-side paging request parameters.
/// </summary>
public record PagedQuery(int Page = 1, int PageSize = 25)
{
    /// <summary>Zero-based offset for EF Core <c>.Skip()</c>.</summary>
    public int Skip => (Page - 1) * PageSize;
}
