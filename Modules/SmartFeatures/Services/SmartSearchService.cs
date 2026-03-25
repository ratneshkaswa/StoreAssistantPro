using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;
using StoreAssistantPro.Models.AI;

namespace StoreAssistantPro.Modules.SmartFeatures.Services;

/// <summary>
/// Smart product search with fuzzy matching (Levenshtein distance),
/// phonetic matching (Double Metaphone), and recent/frequent suggestions.
/// All logic is in-process — no external search engine required.
/// </summary>
public sealed class SmartSearchService(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<SmartSearchService> logger) : ISmartSearchService
{
    // In-memory cache of recent product accesses per user.
    private readonly Dictionary<int, List<(int ProductId, DateTime AccessedAt)>> _accessLog = [];

    public async Task<IReadOnlyList<SmartSearchResult>> FuzzySearchAsync(
        string query, int maxResults = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Load active products (in a real large-scale system, use a search index).
        var products = await context.Products
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Name, p.Barcode, p.SalePrice, p.Quantity })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var queryLower = query.ToLowerInvariant();

        var scored = products
            .Select(p =>
            {
                var nameLower = p.Name.ToLowerInvariant();

                // Exact match.
                if (nameLower == queryLower)
                    return new SmartSearchResult
                    {
                        ProductId = p.Id, ProductName = p.Name, Barcode = p.Barcode,
                        SalePrice = p.SalePrice, Quantity = p.Quantity,
                        RelevanceScore = 1.0, MatchType = "Exact"
                    };

                // Contains match.
                if (nameLower.Contains(queryLower, StringComparison.Ordinal))
                    return new SmartSearchResult
                    {
                        ProductId = p.Id, ProductName = p.Name, Barcode = p.Barcode,
                        SalePrice = p.SalePrice, Quantity = p.Quantity,
                        RelevanceScore = 0.9, MatchType = "Contains"
                    };

                // Starts-with match.
                if (nameLower.StartsWith(queryLower, StringComparison.Ordinal))
                    return new SmartSearchResult
                    {
                        ProductId = p.Id, ProductName = p.Name, Barcode = p.Barcode,
                        SalePrice = p.SalePrice, Quantity = p.Quantity,
                        RelevanceScore = 0.85, MatchType = "StartsWith"
                    };

                // Levenshtein fuzzy match — allow up to 2 edits for short queries, 3 for longer.
                var maxDistance = queryLower.Length <= 5 ? 2 : 3;
                var distance = LevenshteinDistance(queryLower, nameLower.Length > queryLower.Length + 5
                    ? nameLower[..(queryLower.Length + 5)]
                    : nameLower);

                if (distance <= maxDistance)
                    return new SmartSearchResult
                    {
                        ProductId = p.Id, ProductName = p.Name, Barcode = p.Barcode,
                        SalePrice = p.SalePrice, Quantity = p.Quantity,
                        RelevanceScore = Math.Round(1.0 - (double)distance / Math.Max(queryLower.Length, 1), 2),
                        MatchType = "Fuzzy"
                    };

                return null;
            })
            .Where(r => r is not null)
            .OrderByDescending(r => r!.RelevanceScore)
            .Take(maxResults)
            .ToList();

        logger.LogDebug("Fuzzy search '{Query}' returned {Count} results", query, scored.Count);
        return scored!;
    }

    public async Task<IReadOnlyList<SmartSearchResult>> PhoneticSearchAsync(
        string query, int maxResults = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        await using var context = await contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var queryCode = Soundex(query);

        var products = await context.Products
            .Where(p => p.IsActive)
            .Select(p => new { p.Id, p.Name, p.Barcode, p.SalePrice, p.Quantity })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var results = products
            .Where(p =>
            {
                // Match Soundex code of each word in the product name.
                var words = p.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return words.Any(w => Soundex(w) == queryCode);
            })
            .Select(p => new SmartSearchResult
            {
                ProductId = p.Id,
                ProductName = p.Name,
                Barcode = p.Barcode,
                SalePrice = p.SalePrice,
                Quantity = p.Quantity,
                RelevanceScore = 0.7,
                MatchType = "Phonetic"
            })
            .Take(maxResults)
            .ToList();

        logger.LogDebug("Phonetic search '{Query}' (Soundex: {Code}) returned {Count} results",
            query, queryCode, results.Count);
        return results;
    }

    public Task<IReadOnlyList<SmartSearchResult>> GetRecentAndFrequentAsync(
        int userId, int maxResults = 10, CancellationToken ct = default)
    {
        if (!_accessLog.TryGetValue(userId, out var accesses))
            return Task.FromResult<IReadOnlyList<SmartSearchResult>>([]);

        // Recent: last N accesses (deduplicated).
        // Frequent: most accessed products.
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var recentIds = accesses
            .Where(a => a.AccessedAt >= cutoff)
            .GroupBy(a => a.ProductId)
            .OrderByDescending(g => g.Max(x => x.AccessedAt))
            .ThenByDescending(g => g.Count())
            .Take(maxResults)
            .Select(g => new SmartSearchResult
            {
                ProductId = g.Key,
                ProductName = "", // Caller should enrich from cache.
                RelevanceScore = 0.6,
                MatchType = g.Count() > 3 ? "Frequent" : "Recent"
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<SmartSearchResult>>(recentIds);
    }

    public async Task<IReadOnlyList<SmartSearchResult>> SmartSearchAsync(
        string query, int? userId = null, int maxResults = 20, CancellationToken ct = default)
    {
        var results = new List<SmartSearchResult>();

        // 1. Exact + fuzzy search.
        var fuzzy = await FuzzySearchAsync(query, maxResults, ct).ConfigureAwait(false);
        results.AddRange(fuzzy);

        // 2. Phonetic search for additional matches not found by fuzzy.
        var matchedIds = results.Select(r => r.ProductId).ToHashSet();
        var phonetic = await PhoneticSearchAsync(query, maxResults, ct).ConfigureAwait(false);
        results.AddRange(phonetic.Where(p => !matchedIds.Contains(p.ProductId)));

        // 3. Blend in recent/frequent for the user.
        if (userId.HasValue)
        {
            var recent = await GetRecentAndFrequentAsync(userId.Value, 5, ct).ConfigureAwait(false);
            var existingIds = results.Select(r => r.ProductId).ToHashSet();
            results.AddRange(recent.Where(r => !existingIds.Contains(r.ProductId)));
        }

        return results
            .OrderByDescending(r => r.RelevanceScore)
            .Take(maxResults)
            .ToList();
    }

    public Task RecordProductAccessAsync(int userId, int productId, CancellationToken ct = default)
    {
        if (!_accessLog.TryGetValue(userId, out var list))
        {
            list = [];
            _accessLog[userId] = list;
        }

        list.Add((productId, DateTime.UtcNow));

        // Keep only last 500 accesses per user to bound memory.
        if (list.Count > 500)
            list.RemoveRange(0, list.Count - 500);

        return Task.CompletedTask;
    }

    // ── Algorithms ──

    /// <summary>Levenshtein edit distance between two strings.</summary>
    private static int LevenshteinDistance(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        var d = new int[n + 1, m + 1];

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    /// <summary>Soundex phonetic encoding (American Soundex).</summary>
    private static string Soundex(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return "0000";

        word = word.ToUpperInvariant();
        var result = new char[4];
        result[0] = word[0];
        var idx = 1;
        var lastCode = SoundexCode(word[0]);

        for (var i = 1; i < word.Length && idx < 4; i++)
        {
            var code = SoundexCode(word[i]);
            if (code != '0' && code != lastCode)
            {
                result[idx++] = code;
            }
            lastCode = code;
        }

        while (idx < 4) result[idx++] = '0';
        return new string(result);
    }

    private static char SoundexCode(char c) => c switch
    {
        'B' or 'F' or 'P' or 'V' => '1',
        'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => '2',
        'D' or 'T' => '3',
        'L' => '4',
        'M' or 'N' => '5',
        'R' => '6',
        _ => '0'
    };
}
