using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Modules.Startup.ViewModels;
using StoreAssistantPro.Modules.Startup.Views;

namespace StoreAssistantPro.Modules.Startup.Services;

public class SetupWizardFlow(IServiceProvider serviceProvider) : ISetupWizardFlow
{
    public bool RunSetupWizard()
    {
        var window = serviceProvider.GetRequiredService<SetupWizardWindow>();
        return window.ShowDialog() == true;
    }
}
