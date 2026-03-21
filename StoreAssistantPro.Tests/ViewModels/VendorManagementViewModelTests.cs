using NSubstitute;
using StoreAssistantPro.Core.Paging;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Vendors.Services;
using StoreAssistantPro.Modules.Vendors.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public sealed class VendorManagementViewModelTests
{
    private readonly IVendorService _vendorService = Substitute.For<IVendorService>();
    private readonly IVendorLedgerService _ledgerService = Substitute.For<IVendorLedgerService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();
    private readonly IAppStateService _appState = Substitute.For<IAppStateService>();

    private VendorManagementViewModel CreateSut()
    {
        _vendorService.GetPagedAsync(Arg.Any<PagedQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<Vendor>([], 0, 1, 25)));
        _vendorService.CreateAsync(Arg.Any<VendorDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _vendorService.UpdateAsync(Arg.Any<int>(), Arg.Any<VendorDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _vendorService.ToggleActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return new VendorManagementViewModel(_vendorService, _ledgerService, _regional, _appState);
    }

    [Fact]
    public void ClearingSelection_ResetsVendorForm()
    {
        var sut = CreateSut();
        sut.SelectedVendor = new Vendor
        {
            Id = 10,
            Name = "Jaipur Textiles",
            ContactPerson = "Ravi Mehta",
            Phone = "9876543210",
            GSTIN = "08ABCDE1234F1Z5"
        };

        sut.SelectedVendor = null;

        Assert.False(sut.IsEditing);
        Assert.Empty(sut.VendorName);
        Assert.Empty(sut.ContactPerson);
        Assert.Empty(sut.Phone);
        Assert.Empty(sut.GSTIN);
    }

    [Fact]
    public async Task SaveAsync_PreservesSuccessMessage_WhenFormResets()
    {
        var sut = CreateSut();
        sut.VendorName = "Jaipur Textiles";

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Vendor created.", sut.SuccessMessage);
        Assert.False(sut.IsEditing);
        Assert.Empty(sut.VendorName);
    }

    [Fact]
    public async Task ToggleActiveAsync_ReselectsVendorAfterReload()
    {
        var toggled = new Vendor { Id = 10, Name = "Jaipur Textiles", IsActive = false };
        _vendorService.GetPagedAsync(Arg.Any<PagedQuery>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<Vendor>(new[] { toggled }, 1, 1, 25)));
        _vendorService.ToggleActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new VendorManagementViewModel(_vendorService, _ledgerService, _regional, _appState);
        sut.SelectedVendor = new Vendor { Id = 10, Name = "Jaipur Textiles", IsActive = true };

        await sut.ToggleActiveCommand.ExecuteAsync(sut.SelectedVendor);

        Assert.NotNull(sut.SelectedVendor);
        Assert.Equal(10, sut.SelectedVendor!.Id);
        Assert.Equal("Status toggled.", sut.SuccessMessage);
    }

    [Fact]
    public async Task SearchAsync_ClearsSelection_WhenResultSetChanges()
    {
        var sut = CreateSut();

        sut.SelectedVendor = new Vendor { Id = 55, Name = "Old Vendor" };
        sut.SearchText = "jaipur";

        await sut.SearchCommand.ExecuteAsync(null);

        Assert.Null(sut.SelectedVendor);
    }
}
