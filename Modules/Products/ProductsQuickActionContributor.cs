using CommunityToolkit.Mvvm.Input;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Modules.Products;

/// <summary>
/// Registers Products-module quick actions into the POS toolbar.
/// Resolved automatically by <c>MainViewModel</c> via
/// <c>IEnumerable&lt;IQuickActionContributor&gt;</c>.
/// </summary>
public class ProductsQuickActionContributor(INavigationService navigationService) : IQuickActionContributor
{
    public void Contribute(IQuickActionService service)
    {
        service.Register(new QuickAction
        {
            Title = "Add Product",
            Icon = "➕",
            Command = new RelayCommand(() => navigationService.NavigateTo("Products")),
            ShortcutText = "Ctrl+N",
            SortOrder = 25
        });
    }
}
