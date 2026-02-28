namespace StoreAssistantPro.Core.Data;

/// <summary>
/// Carries one page of <typeparamref name="T"/> items together with
/// paging metadata. Services return this from paged queries so
/// ViewModels can display page navigation without holding the entire
/// dataset in memory.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int PageIndex,
    int PageSize)
{
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 0;

    public bool HasPreviousPage => PageIndex > 0;
    public bool HasNextPage => PageIndex < TotalPages - 1;

    public static PagedResult<T> Empty(int pageSize = 50) =>
        new([], 0, 0, pageSize);
}
