using StoreAssistantPro.Models.AI;

namespace StoreAssistantPro.Modules.SmartFeatures.Services;

/// <summary>
/// Fuzzy, phonetic, and intelligent product search.
/// Features #531–534: fuzzy search, phonetic matching,
/// recent/frequent suggestions, barcode OCR from camera.
/// </summary>
public interface ISmartSearchService
{
    /// <summary>Search products with typo tolerance. (#531)</summary>
    Task<IReadOnlyList<SmartSearchResult>> FuzzySearchAsync(
        string query, int maxResults = 20, CancellationToken ct = default);

    /// <summary>Search by pronunciation (Soundex/Metaphone). (#532)</summary>
    Task<IReadOnlyList<SmartSearchResult>> PhoneticSearchAsync(
        string query, int maxResults = 20, CancellationToken ct = default);

    /// <summary>Get recently/frequently accessed products for the current user. (#533)</summary>
    Task<IReadOnlyList<SmartSearchResult>> GetRecentAndFrequentAsync(
        int userId, int maxResults = 10, CancellationToken ct = default);

    /// <summary>Combined smart search: exact → fuzzy → phonetic → recent/frequent. (#531–533)</summary>
    Task<IReadOnlyList<SmartSearchResult>> SmartSearchAsync(
        string query, int? userId = null, int maxResults = 20, CancellationToken ct = default);

    /// <summary>Record a product access for recent/frequent tracking.</summary>
    Task RecordProductAccessAsync(int userId, int productId, CancellationToken ct = default);

    /// <summary>Look up a product by scanned barcode image text (OCR output). (#534)</summary>
    Task<IReadOnlyList<SmartSearchResult>> BarcodeOcrSearchAsync(
        string ocrText, int maxResults = 5, CancellationToken ct = default);
}
