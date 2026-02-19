namespace StoreAssistantPro.Services;

public interface IStartupService
{
    Task<bool> IsAppInitializedAsync();
}
