using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Users.Services;

public interface IUserService
{
    Task<IEnumerable<UserCredential>> GetAllUsersAsync(CancellationToken ct = default);
    Task ChangePinAsync(UserType userType, string newPin, CancellationToken ct = default);

    /// <summary>Import users from CSV rows (#287).</summary>
    Task<int> ImportUsersAsync(IReadOnlyList<Dictionary<string, string>> rows, CancellationToken ct = default);

    /// <summary>Export all users as flat records (#288).</summary>
    Task<IReadOnlyList<UserExportRow>> ExportUsersAsync(CancellationToken ct = default);

    /// <summary>Update user display name and contact info (#281).</summary>
    Task UpdateProfileAsync(UserType userType, string? displayName, string? email, string? phone, CancellationToken ct = default);
}

public record UserExportRow(
    string UserType,
    string? DisplayName,
    bool IsActive,
    string? Email,
    string? Phone);
