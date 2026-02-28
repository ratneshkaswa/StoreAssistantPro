using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Prompts the user via <see cref="IDialogService.PromptPassword"/>
/// and validates against the stored master PIN hash via
/// <see cref="ILoginService.ValidateMasterPinAsync"/>.
/// </summary>
public class MasterPinValidator(
    IDialogService dialogService,
    ILoginService loginService) : IMasterPinValidator
{
    public async Task<bool> ValidateAsync(string promptMessage = "Enter Master PIN to continue.")
    {
        var pin = dialogService.PromptPassword(promptMessage);

        if (string.IsNullOrWhiteSpace(pin))
            return false;

        return await loginService.ValidateMasterPinAsync(pin);
    }
}
