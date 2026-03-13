using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Inward.Services;
using StoreAssistantPro.Modules.Inward.ViewModels;
using StoreAssistantPro.Modules.Products.Services;
using StoreAssistantPro.Modules.Vendors.Services;
using System.Collections.ObjectModel;

namespace StoreAssistantPro.Tests.ViewModels;

public class InwardEntryViewModelTests
{
    private readonly IInwardService _inwardService = Substitute.For<IInwardService>();
    private readonly IVendorService _vendorService = Substitute.For<IVendorService>();
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly IRegionalSettingsService _regional = Substitute.For<IRegionalSettingsService>();

    private InwardEntryViewModel CreateSut() => new(_inwardService, _vendorService, _productService, _regional);

    [Fact]
    public async Task Next_InvalidParcelCount_UsesCleanValidationMessage()
    {
        var sut = CreateSut();
        sut.ParcelCount = 11;

        await sut.NextCommand.ExecuteAsync(null);

        Assert.Equal("Select 1-10 parcels.", sut.ErrorMessage);
    }

    [Fact]
    public async Task Next_InvalidTransportCharge_UsesCleanValidationMessage()
    {
        var sut = CreateSut();
        sut.CurrentStep = 2;
        sut.TransportCharges = "-5";

        await sut.NextCommand.ExecuteAsync(null);

        Assert.Equal("Enter a valid transport charge (0 or more).", sut.ErrorMessage);
    }

    [Fact]
    public async Task Next_FromTransportStep_Generates_One_Editable_Row_Per_Parcel()
    {
        var now = new DateTime(2026, 3, 13, 10, 0, 0);
        _regional.Now.Returns(now);
        _inwardService.GenerateParcelNumbersAsync(now, 2, Arg.Any<CancellationToken>())
            .Returns(["03-01", "03-02"]);

        var sut = CreateSut();
        sut.ParcelCount = 2;

        await sut.NextCommand.ExecuteAsync(null);
        sut.TransportCharges = "200";
        await sut.NextCommand.ExecuteAsync(null);

        Assert.Equal(3, sut.CurrentStep);
        Assert.Equal(2, sut.Parcels.Count);
        Assert.All(sut.Parcels, parcel => Assert.Single(parcel.ProductRows));
        Assert.Equal(100m, sut.Parcels[0].TransportCharge);
        Assert.Equal(100m, sut.Parcels[1].TransportCharge);
    }

    [Fact]
    public async Task Save_WithPartialRowWithoutProduct_Shows_RowSpecific_Message()
    {
        _regional.Now.Returns(new DateTime(2026, 3, 13, 10, 0, 0));

        var sut = CreateSut();
        var parcel = new ParcelEntryModel { ParcelNumber = "03-01" };
        parcel.AddProductRowCommand.Execute(null);
        parcel.ProductRows[0].Quantity = "4";
        sut.Parcels = new ObservableCollection<ParcelEntryModel> { parcel };

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Select a product for parcel 03-01, row 1.", sut.ErrorMessage);
        await _inwardService.DidNotReceive().CreateAsync(Arg.Any<InwardEntryDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Save_ValidEntry_ResetsWizardState()
    {
        var now = new DateTime(2026, 3, 13, 10, 0, 0);
        _regional.Now.Returns(now);
        _inwardService.CreateAsync(Arg.Any<InwardEntryDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        sut.CurrentStep = 3;
        sut.ParcelCount = 2;
        sut.TransportCharges = "200";
        sut.ParcelNumbers = ["03-01"];

        var parcel = new ParcelEntryModel { ParcelNumber = "03-01" };
        parcel.AddProductRowCommand.Execute(null);
        parcel.ProductRows[0].SelectedProduct = new Product { Id = 11, Name = "Oxford Shirt" };
        parcel.ProductRows[0].Quantity = "3";
        sut.Parcels = new ObservableCollection<ParcelEntryModel> { parcel };

        await sut.SaveCommand.ExecuteAsync(null);

        Assert.Equal("Inward entry saved successfully.", sut.SuccessMessage);
        Assert.Equal(1, sut.CurrentStep);
        Assert.Equal(1, sut.ParcelCount);
        Assert.Equal("0", sut.TransportCharges);
        Assert.Empty(sut.Parcels);
        Assert.Empty(sut.ParcelNumbers);
    }
}
