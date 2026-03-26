namespace StoreAssistantPro.Models.Api;

/// <summary>
/// JWT token pair returned after successful API authentication.
/// </summary>
public sealed record ApiTokenResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string TokenType = "Bearer");

/// <summary>
/// API client credentials for authentication.
/// </summary>
public sealed record ApiClientCredentials(
    string ClientId,
    string ClientSecret,
    string[] Scopes);

/// <summary>
/// Rate limit status for an API client.
/// </summary>
public sealed record RateLimitStatus(
    string ClientId,
    int RequestsRemaining,
    int RequestLimit,
    TimeSpan WindowDuration,
    DateTime WindowResetAt,
    bool IsThrottled);
