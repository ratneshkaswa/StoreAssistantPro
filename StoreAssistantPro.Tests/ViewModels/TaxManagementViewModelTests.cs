using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Tax.Services;
using StoreAssistantPro.Modules.Tax.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public sealed class TaxManagementViewModelTests
{
    private readonly ITaxService _taxService = Substitute.For<ITaxService>();
    private readonly ITaxGroupService _taxGroupService = Substitute.For<ITaxGroupService>();
    private readonly IRegionalSettingsService _regionalSettings = Substitute.For<IRegionalSettingsService>();

    private TaxManagementViewModel CreateSut()
    {
        _taxService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxMaster>>(Array.Empty<TaxMaster>()));
        _taxService.CreateAsync(Arg.Any<TaxDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxService.UpdateAsync(Arg.Any<int>(), Arg.Any<TaxDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxService.DeleteAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _taxGroupService.GetAllGroupsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxGroup>>(Array.Empty<TaxGroup>()));
        _taxGroupService.GetAllHSNCodesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<HSNCode>>(Array.Empty<HSNCode>()));
        _taxGroupService.CreateGroupAsync(Arg.Any<TaxGroupDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxGroupService.UpdateGroupAsync(Arg.Any<int>(), Arg.Any<TaxGroupDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxGroupService.ToggleGroupActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxGroupService.CreateSlabAsync(Arg.Any<TaxSlabDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxGroupService.UpdateSlabAsync(Arg.Any<int>(), Arg.Any<TaxSlabDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxGroupService.DeleteSlabAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxGroupService.CreateHSNCodeAsync(Arg.Any<HSNCodeDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxGroupService.UpdateHSNCodeAsync(Arg.Any<int>(), Arg.Any<HSNCodeDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _taxGroupService.ToggleHSNActiveAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _regionalSettings.Now.Returns(new DateTime(2026, 3, 15, 10, 0, 0));

        return new TaxManagementViewModel(_taxService, _taxGroupService, _regionalSettings);
    }

    [Fact]
    public async Task SaveRateAsync_PreservesSuccessMessage_WhenFormResets()
    {
        var sut = CreateSut();
        sut.TaxName = "GST 5%";
        sut.SlabPercent = "5";

        await sut.SaveRateCommand.ExecuteAsync(null);

        Assert.Equal("Tax rate created.", sut.SuccessMessage);
        Assert.False(sut.IsEditing);
        Assert.Empty(sut.TaxName);
        Assert.Empty(sut.SlabPercent);
        Assert.Null(sut.SelectedTax);
    }

    [Fact]
    public async Task AddSlabAsync_UsesUpdatePath_WhenASlabIsSelected()
    {
        var sut = CreateSut();
        var existingSlab = new TaxSlab
        {
            Id = 17,
            TaxGroupId = 4,
            GSTPercent = 5m,
            PriceFrom = 0m,
            PriceTo = 1000m
        };
        var groupAfterReload = new TaxGroup
        {
            Id = 4,
            Name = "GST Garments",
            Slabs = new List<TaxSlab>
            {
                new()
                {
                    Id = existingSlab.Id,
                    TaxGroupId = 4,
                    GSTPercent = 12m,
                    PriceFrom = 1001m,
                    PriceTo = TaxSlab.MaxPrice
                }
            }
        };

        _taxGroupService.GetAllGroupsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxGroup>>(new[] { groupAfterReload }));

        sut.SelectedGroup = new TaxGroup
        {
            Id = 4,
            Name = "GST Garments",
            Slabs = new List<TaxSlab> { existingSlab }
        };
        sut.SelectedSlab = existingSlab;

        Assert.Equal("Update Slab", sut.SlabActionText);
        Assert.Equal("5", sut.SlabGST);
        Assert.Equal("0", sut.SlabPriceFrom);
        Assert.Equal("1000", sut.SlabPriceTo);

        sut.SlabGST = "12";
        sut.SlabPriceFrom = "1001";
        sut.SlabPriceTo = string.Empty;

        await sut.AddSlabCommand.ExecuteAsync(null);

        await _taxGroupService.Received(1).UpdateSlabAsync(
            existingSlab.Id,
            Arg.Is<TaxSlabDto>(dto =>
                dto.TaxGroupId == 4
                && dto.GSTPercent == 12m
                && dto.PriceFrom == 1001m
                && dto.PriceTo == TaxSlab.MaxPrice),
            Arg.Any<CancellationToken>());
        await _taxGroupService.DidNotReceive().CreateSlabAsync(Arg.Any<TaxSlabDto>(), Arg.Any<CancellationToken>());
        Assert.Equal("Slab updated.", sut.SuccessMessage);
        Assert.Null(sut.SelectedSlab);
        Assert.Equal("Add Slab", sut.SlabActionText);
        Assert.Empty(sut.SlabGST);
        Assert.Empty(sut.SlabPriceFrom);
        Assert.Empty(sut.SlabPriceTo);
    }

    [Fact]
    public void HsnCodeValue_ShouldNormalizeToDigitsAndCapLength()
    {
        var sut = CreateSut();

        sut.HSNCodeValue = "61A0-9 7B12345";

        Assert.Equal("61097123", sut.HSNCodeValue);
    }

    [Fact]
    public async Task SaveHsnAsync_ShouldRequireFourToEightDigits()
    {
        var sut = CreateSut();
        sut.HSNCodeValue = "12A";
        sut.HSNDescription = "T-shirts";

        await sut.SaveHSNCommand.ExecuteAsync(null);

        Assert.Equal("HSN code must be 4-8 digits.", sut.ErrorMessage);
        await _taxGroupService.DidNotReceive()
            .CreateHSNCodeAsync(Arg.Any<HSNCodeDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleGroupActiveAsync_ShouldReloadAndReselectCurrentGroup()
    {
        var sut = CreateSut();
        var selectedGroup = new TaxGroup
        {
            Id = 8,
            Name = "GST Readywear",
            IsActive = true,
            Slabs = new List<TaxSlab>()
        };

        _taxGroupService.GetAllGroupsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<TaxGroup>>(new[]
            {
                new TaxGroup
                {
                    Id = selectedGroup.Id,
                    Name = selectedGroup.Name,
                    IsActive = false,
                    Slabs = new List<TaxSlab>()
                }
            }));

        sut.SelectedGroup = selectedGroup;

        await sut.ToggleGroupActiveCommand.ExecuteAsync(selectedGroup);

        Assert.NotNull(sut.SelectedGroup);
        Assert.Equal(selectedGroup.Id, sut.SelectedGroup!.Id);
        Assert.Equal("Group 'GST Readywear' deactivated.", sut.SuccessMessage);
    }
}
