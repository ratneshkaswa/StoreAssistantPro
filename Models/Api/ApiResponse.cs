namespace StoreAssistantPro.Models.Api;

/// <summary>
/// Standard API response wrapper for REST endpoints.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }
    public string? RequestId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Paginated API response for list endpoints.
/// </summary>
public sealed class PagedApiResponse<T>
{
    public bool Success { get; init; }
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public string? RequestId { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
