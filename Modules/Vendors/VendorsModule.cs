using Microsoft.Extensions.DependencyInjection;
using StoreAssistantPro.Core.Navigation;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Vendors.Services;
using StoreAssistantPro.Modules.Vendors.ViewModels;

namespace StoreAssistantPro.Modules.Vendors;

public static class VendorsModule
{
    public const string VendorsPage = "Vendors";

    public static IServiceCollection AddVendorsModule(
        this IServiceCollection services,
        NavigationPageRegistry pageRegistry,
        IFocusMapRegistry focusMapRegistry)
    {
        pageRegistry.Map<VendorsViewModel>(VendorsPage);

        // ── Focus map: logical focus order for the Vendors page ──
        //
        // Toolbar → Search → DataGrid → Add-form fields → Save/Cancel
        //
        // The PredictiveFocusService uses this map to:
        //   - Land on SearchBox after page navigation
        //   - Land on NameInput after "Add Vendor" button click
        //   - Advance to SaveButton after the last form field
        focusMapRegistry.Register(FocusMap.For(VendorsPage)
            .Add("VendorSearchBox",   FocusRole.SearchInput)
            .Add("VendorDataGrid",    FocusRole.DataGrid)
            .Add("NameInput",         FocusRole.PrimaryInput,    isAvailableWhen: "IsAddFormVisible")
            .Add("ContactInput",      FocusRole.FormField,       isAvailableWhen: "IsAddFormVisible")
            .Add("PhoneInput",        FocusRole.FormField,       isAvailableWhen: "IsAddFormVisible")
            .Add("EmailInput",        FocusRole.FormField,       isAvailableWhen: "IsAddFormVisible")
            .Add("GSTINInput",        FocusRole.FormField,       isAvailableWhen: "IsAddFormVisible")
            .Add("SaveButton",        FocusRole.PrimaryAction,   isAvailableWhen: "IsAddFormVisible")
            .Add("CancelButton",      FocusRole.SecondaryAction, isAvailableWhen: "IsAddFormVisible")
            .Build());

        services.AddTransient<IVendorService, VendorService>();
        services.AddTransient<VendorsViewModel>();

        return services;
    }
}
