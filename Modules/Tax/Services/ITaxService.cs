using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Tax.Services;

public interface ITaxService
{
    Task<IReadOnlyList<TaxProfile>> GetAllProfilesAsync();
    Task<TaxProfile> GetProfileWithItemsAsync(int profileId);
    Task AddProfileAsync(TaxProfile profile);
    Task UpdateProfileAsync(TaxProfile profile);
    Task SetActiveAsync(int profileId, bool isActive);
    Task<bool> IsProfileUsedByProductsAsync(int profileId);
}
