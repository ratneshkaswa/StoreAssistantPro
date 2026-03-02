using NSubstitute;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Firm.Events;
using StoreAssistantPro.Modules.Firm.Services;
using StoreAssistantPro.Modules.Firm.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class FirmViewModelTests
{
    private readonly IFirmService _firmService = Substitute.For<IFirmService>();
    private readonly IEventBus _eventBus = Substitute.For<IEventBus>();

    private FirmViewModel CreateSut() => new(_firmService, _eventBus);

    [Fact]
    public async Task LoadFirm_PopulatesFields()
    {
        _firmService.GetFirmAsync(Arg.Any<CancellationToken>()).Returns(new AppConfig
        {
            FirmName = "Test Store",
            Address = "123 Main St",
            State = "Rajasthan",
            Pincode = "302001",
            Phone = "9876543210",
            Email = "test@store.com",
            GSTNumber = "08ABCDE1234F1Z5",
            PANNumber = "ABCDE1234F",
            CurrencySymbol = "Rs.",
            FinancialYearStartMonth = 4,
            DateFormat = "dd-MM-yyyy"
        });

        var sut = CreateSut();
        await sut.LoadFirmCommand.ExecuteAsync(null);

        Assert.Equal("Test Store", sut.FirmName);
        Assert.Equal("123 Main St", sut.Address);
        Assert.Equal("Rajasthan", sut.State);
        Assert.Equal("302001", sut.Pincode);
        Assert.Equal("9876543210", sut.Phone);
        Assert.Equal("test@store.com", sut.Email);
        Assert.Equal("08ABCDE1234F1Z5", sut.GSTNumber);
        Assert.Equal("ABCDE1234F", sut.PANNumber);
        Assert.Equal("Rs.", sut.SelectedCurrencySymbol);
        Assert.Equal("April", sut.SelectedFYStartMonth);
        Assert.Equal("dd-MM-yyyy", sut.SelectedDateFormat);
    }

    [Fact]
    public async Task LoadFirm_NullConfig_DoesNotThrow()
    {
        _firmService.GetFirmAsync(Arg.Any<CancellationToken>()).Returns((AppConfig?)null);

        var sut = CreateSut();
        await sut.LoadFirmCommand.ExecuteAsync(null);

        Assert.Empty(sut.FirmName);
    }

    [Fact]
    public async Task SaveFirm_EmptyName_HasValidationErrors()
    {
        var sut = CreateSut();
        sut.FirmName = "   ";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        Assert.True(sut.HasErrors);
        await _firmService.DidNotReceive().UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveFirm_ValidInput_CallsServiceAndShowsSuccess()
    {
        var sut = CreateSut();
        sut.FirmName = "Updated Store";
        sut.Address = "456 Oak Ave";
        sut.State = "Gujarat";
        sut.Pincode = "380001";
        sut.Phone = "9876543210";
        sut.Email = "info@updated.com";
        sut.GSTNumber = "";
        sut.PANNumber = "";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        await _firmService.Received(1).UpdateFirmAsync(Arg.Is<FirmUpdateDto>(d =>
            d.FirmName == "Updated Store" &&
            d.Address == "456 Oak Ave" &&
            d.State == "Gujarat" &&
            d.Pincode == "380001" &&
            d.Phone == "9876543210" &&
            d.CurrencySymbol == "\u20B9" &&
            d.DateFormat == "dd/MM/yyyy" &&
            d.NumberFormat == "Indian"), Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Is<FirmUpdatedEvent>(e =>
            e.FirmName == "Updated Store" &&
            e.CurrencySymbol == "\u20B9" &&
            e.DateFormat == "dd/MM/yyyy"));
        Assert.Equal("Firm information saved.", sut.SuccessMessage);
        Assert.Empty(sut.ErrorMessage);
    }

    [Fact]
    public async Task SaveFirm_ServiceThrows_SetsError()
    {
        _firmService.UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB error")));

        var sut = CreateSut();
        sut.FirmName = "Store";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        Assert.Equal("DB error", sut.ErrorMessage);
        Assert.Empty(sut.SuccessMessage);
    }

    [Fact]
    public void WizardNavigation_NextAndBack()
    {
        var sut = CreateSut();
        sut.FirmName = "Test Store";

        Assert.Equal(1, sut.CurrentStep);
        Assert.True(sut.IsStep1);
        Assert.False(sut.CanGoBack);
        Assert.True(sut.CanGoNext);

        sut.NextCommand.Execute(null);
        Assert.Equal(2, sut.CurrentStep);
        Assert.True(sut.IsStep2);
        Assert.True(sut.CanGoBack);

        sut.NextCommand.Execute(null);
        Assert.Equal(3, sut.CurrentStep);
        Assert.True(sut.IsStep3);
        Assert.True(sut.IsLastStep);
        Assert.False(sut.CanGoNext);

        sut.BackCommand.Execute(null);
        Assert.Equal(2, sut.CurrentStep);
    }

    [Fact]
    public async Task SaveFirm_IncludesFinancialYear()
    {
        _firmService.UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var sut = CreateSut();
        sut.FirmName = "FY Test";
        sut.SelectedFYStartMonth = "July";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        await _firmService.Received(1).UpdateFirmAsync(Arg.Is<FirmUpdateDto>(d =>
            d.FinancialYearStartMonth == 7 &&
            d.FinancialYearEndMonth == 6), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void FinancialYearDisplay_UpdatesWithMonth()
    {
        var sut = CreateSut();
        sut.SelectedFYStartMonth = "January";
        Assert.Equal("January \u2013 December", sut.FinancialYearDisplay);

        sut.SelectedFYStartMonth = "April";
        Assert.Equal("April \u2013 March", sut.FinancialYearDisplay);
    }

    [Fact]
    public void Constructor_DefaultCurrencySymbolIsRupee()
    {
        var sut = CreateSut();
        Assert.Equal("\u20B9", sut.SelectedCurrencySymbol);
    }

    [Fact]
    public void NextAndBack_ClearMessages()
    {
        var sut = CreateSut();
        sut.FirmName = "Test";

        sut.NextCommand.Execute(null);
        Assert.Equal(2, sut.CurrentStep);
        Assert.Empty(sut.ErrorMessage);
        Assert.Empty(sut.SuccessMessage);

        sut.BackCommand.Execute(null);
        Assert.Equal(1, sut.CurrentStep);
        Assert.Empty(sut.ErrorMessage);
        Assert.Empty(sut.SuccessMessage);
    }

    [Fact]
    public void Next_WithValidationErrors_StaysOnStep()
    {
        var sut = CreateSut();
        sut.FirmName = "";

        sut.NextCommand.Execute(null);

        Assert.Equal(1, sut.CurrentStep);
        Assert.True(sut.HasErrors);
        Assert.Equal("Please fix the highlighted fields.", sut.ErrorMessage);
    }

    [Fact]
    public void Next_CrossStepValidation_DoesNotBleed()
    {
        // Step 1 valid, go to Step 2
        var sut = CreateSut();
        sut.FirmName = "Test Store";
        sut.NextCommand.Execute(null);
        Assert.Equal(2, sut.CurrentStep);

        // Enter invalid PAN on Step 2
        sut.PANNumber = "BAD";

        // Go back to Step 1
        sut.BackCommand.Execute(null);
        Assert.Equal(1, sut.CurrentStep);

        // Next must succeed — Step 2's PAN error must NOT block Step 1
        sut.NextCommand.Execute(null);
        Assert.Equal(2, sut.CurrentStep);
        Assert.Empty(sut.ErrorMessage);
    }

    [Fact]
    public void Next_OptionalFieldErrors_DoNotBlockNavigation()
    {
        var sut = CreateSut();
        sut.FirmName = "Test Store";

        // Enter invalid optional fields on Step 1
        sut.Email = "not-an-email";
        sut.Pincode = "123";

        // Next must succeed — optional format errors don't block
        sut.NextCommand.Execute(null);
        Assert.Equal(2, sut.CurrentStep);

        // Enter invalid optional fields on Step 2
        sut.GSTNumber = "BAD";
        sut.PANNumber = "XY";

        // Next must succeed — optional format errors don't block
        sut.NextCommand.Execute(null);
        Assert.Equal(3, sut.CurrentStep);
    }
}
