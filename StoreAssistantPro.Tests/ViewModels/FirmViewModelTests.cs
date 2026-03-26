using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StoreAssistantPro.Core.Events;
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
    public async Task LoadFirm_PopulatesUnifiedFields()
    {
        _firmService.GetFirmAsync(Arg.Any<CancellationToken>()).Returns(new FirmManagementSnapshot(
            FirmName: "Sonali Collection",
            Address: "12 Station Road",
            State: "Maharashtra",
            Pincode: "400001",
            Phone: "9876543210",
            Email: "firm@example.com",
            GSTNumber: "27AAPFU0939F1ZV",
            PANNumber: "AAPFU0939F",
            GstRegistrationType: "Composition",
            CompositionSchemeRate: 1.5m,
            StateCode: "27",
            FinancialYearStartMonth: 7,
            FinancialYearEndMonth: 6,
            CurrencySymbol: "Rs.",
            DateFormat: "yyyy-MM-dd",
            NumberFormat: "Indian",
            DefaultTaxMode: "Inclusive",
            RoundingMethod: "NearestFive",
            NegativeStockAllowed: true,
            NumberToWordsLanguage: "Hindi",
            InvoicePrefix: "INV",
            ReceiptFooterText: "Thank you! Visit again!",
            LogoPath: string.Empty,
            BankName: "ICICI Bank",
            BankAccountNumber: "1234567890",
            BankIFSC: "ICIC0001234",
            ReceiptHeaderText: "Welcome",
            InvoiceResetPeriod: "Annually"));

        var sut = CreateSut();

        await sut.LoadFirmCommand.ExecuteAsync(null);

        Assert.Equal("Sonali Collection", sut.FirmName);
        Assert.Equal("12 Station Road", sut.Address);
        Assert.Equal("Maharashtra", sut.State);
        Assert.Equal("400001", sut.Pincode);
        Assert.Equal("9876543210", sut.Phone);
        Assert.Equal("firm@example.com", sut.Email);
        Assert.Equal("27AAPFU0939F1ZV", sut.GSTNumber);
        Assert.Equal("AAPFU0939F", sut.PANNumber);
        Assert.Equal("Composition", sut.SelectedGstRegistrationType);
        Assert.Equal("1.5", sut.CompositionRate);
        Assert.Equal("Rs.", sut.SelectedCurrencySymbol);
        Assert.Equal("July", sut.SelectedFYStartMonth);
        Assert.Equal("yyyy-MM-dd", sut.SelectedDateFormat);
        Assert.Equal("Tax-Inclusive (MRP)", sut.SelectedTaxMode);
        Assert.Equal("Round to nearest \u20B95", sut.SelectedRoundingMethod);
        Assert.Equal("Hindi", sut.SelectedNumberToWordsLanguage);
        Assert.True(sut.NegativeStockAllowed);
        Assert.False(sut.IsDirty);
    }

    [Fact]
    public async Task LoadFirm_WithStateCodeFallback_UsesCanonicalState()
    {
        _firmService.GetFirmAsync(Arg.Any<CancellationToken>()).Returns(new FirmManagementSnapshot(
            FirmName: "Firm",
            Address: string.Empty,
            State: string.Empty,
            Pincode: string.Empty,
            Phone: string.Empty,
            Email: string.Empty,
            GSTNumber: null,
            PANNumber: null,
            GstRegistrationType: "Regular",
            CompositionSchemeRate: 1m,
            StateCode: "08",
            FinancialYearStartMonth: 4,
            FinancialYearEndMonth: 3,
            CurrencySymbol: "\u20B9",
            DateFormat: "dd/MM/yyyy",
            NumberFormat: "Indian",
            DefaultTaxMode: "Exclusive",
            RoundingMethod: "None",
            NegativeStockAllowed: false,
            NumberToWordsLanguage: "English",
            InvoicePrefix: "INV",
            ReceiptFooterText: "Thank you! Visit again!",
            LogoPath: string.Empty,
            BankName: string.Empty,
            BankAccountNumber: string.Empty,
            BankIFSC: string.Empty,
            ReceiptHeaderText: string.Empty,
            InvoiceResetPeriod: "Never"));

        var sut = CreateSut();

        await sut.LoadFirmCommand.ExecuteAsync(null);

        Assert.Equal("Rajasthan", sut.State);
        Assert.False(sut.IsDirty);
    }

    [Fact]
    public async Task LoadFirm_NullSnapshot_LeavesDefaults()
    {
        _firmService.GetFirmAsync(Arg.Any<CancellationToken>()).Returns((FirmManagementSnapshot?)null);

        var sut = CreateSut();

        await sut.LoadFirmCommand.ExecuteAsync(null);

        Assert.Empty(sut.FirmName);
        Assert.Equal("April", sut.SelectedFYStartMonth);
        Assert.Equal("\u20B9", sut.SelectedCurrencySymbol);
        Assert.False(sut.IsDirty);
    }

    [Fact]
    public async Task SaveFirm_EmptyName_BlocksSave()
    {
        var sut = CreateSut();
        sut.FirmName = "   ";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        Assert.True(sut.HasErrors);
        Assert.True(sut.HasValidationErrors);
        Assert.Contains("Firm name is required.", sut.ValidationErrors);
        Assert.Equal(nameof(FirmViewModel.FirmName), sut.FirstErrorFieldKey);
        Assert.Equal("Review the highlighted business fields before saving.", sut.ErrorMessage);
        await _firmService.DidNotReceive().UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveFirm_ValidInput_PersistsUnifiedBusinessSettings()
    {
        _firmService.UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _eventBus.PublishAsync(Arg.Any<FirmUpdatedEvent>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        sut.FirmName = "  Updated Store  ";
        sut.Address = "456 Oak Avenue";
        sut.State = "Maharashtra";
        sut.Pincode = "400001";
        sut.Phone = "9876543210";
        sut.Email = "info@updated.com";
        sut.GSTNumber = "27AAPFU0939F1ZV";
        sut.PANNumber = "AAPFU0939F";
        sut.SelectedGstRegistrationType = "Composition";
        sut.CompositionRate = "1.5";
        sut.SelectedCurrencySymbol = "Rs.";
        sut.SelectedFYStartMonth = "July";
        sut.SelectedDateFormat = "yyyy-MM-dd";
        sut.SelectedTaxMode = "Tax-Inclusive (MRP)";
        sut.SelectedRoundingMethod = "Round to nearest \u20B95";
        sut.SelectedNumberToWordsLanguage = "Hindi";
        sut.NegativeStockAllowed = true;

        await sut.SaveFirmCommand.ExecuteAsync(null);

        await _firmService.Received(1).UpdateFirmAsync(Arg.Is<FirmUpdateDto>(dto =>
            dto.FirmName == "Updated Store"
            && dto.Address == "456 Oak Avenue"
            && dto.State == "Maharashtra"
            && dto.Pincode == "400001"
            && dto.Phone == "9876543210"
            && dto.Email == "info@updated.com"
            && dto.GSTNumber == "27AAPFU0939F1ZV"
            && dto.PANNumber == "AAPFU0939F"
            && dto.GstRegistrationType == "Composition"
            && dto.CompositionSchemeRate == 1.5m
            && dto.StateCode == "27"
            && dto.FinancialYearStartMonth == 7
            && dto.FinancialYearEndMonth == 6
            && dto.CurrencySymbol == "Rs."
            && dto.DateFormat == "yyyy-MM-dd"
            && dto.NumberFormat == "Indian"
            && dto.DefaultTaxMode == "Inclusive"
            && dto.RoundingMethod == "NearestFive"
            && dto.NegativeStockAllowed
            && dto.NumberToWordsLanguage == "Hindi"), Arg.Any<CancellationToken>());
        await _eventBus.Received(1).PublishAsync(Arg.Is<FirmUpdatedEvent>(e =>
            e.FirmName == "Updated Store"
            && e.CurrencySymbol == "Rs."
            && e.DateFormat == "yyyy-MM-dd"));
        Assert.Equal("Business settings saved.", sut.SuccessMessage);
        Assert.Empty(sut.ErrorMessage);
        Assert.False(sut.IsDirty);
    }

    [Fact]
    public async Task SaveFirm_AfterLoad_PreservesHiddenSettlementFields()
    {
        _firmService.GetFirmAsync(Arg.Any<CancellationToken>()).Returns(new FirmManagementSnapshot(
            FirmName: "Store",
            Address: string.Empty,
            State: "Rajasthan",
            Pincode: string.Empty,
            Phone: "9876543210",
            Email: string.Empty,
            GSTNumber: null,
            PANNumber: null,
            GstRegistrationType: "Regular",
            CompositionSchemeRate: 0m,
            StateCode: "08",
            FinancialYearStartMonth: 4,
            FinancialYearEndMonth: 3,
            CurrencySymbol: "\u20B9",
            DateFormat: "dd/MM/yyyy",
            NumberFormat: "Indian",
            DefaultTaxMode: "Exclusive",
            RoundingMethod: "None",
            NegativeStockAllowed: false,
            NumberToWordsLanguage: "English",
            InvoicePrefix: "INV",
            ReceiptFooterText: "Thank you! Visit again!",
            LogoPath: string.Empty,
            BankName: "Axis Bank",
            BankAccountNumber: "9988776655",
            BankIFSC: "UTIB0000012",
            ReceiptHeaderText: "Retail Copy",
            InvoiceResetPeriod: "Annually"));
        _firmService.UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _eventBus.PublishAsync(Arg.Any<FirmUpdatedEvent>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        await sut.LoadFirmCommand.ExecuteAsync(null);
        sut.Phone = "9123456789";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        await _firmService.Received(1).UpdateFirmAsync(Arg.Is<FirmUpdateDto>(dto =>
            dto.BankName == "Axis Bank"
            && dto.BankAccountNumber == "9988776655"
            && dto.BankIFSC == "UTIB0000012"
            && dto.ReceiptHeaderText == "Retail Copy"
            && dto.InvoiceResetPeriod == "Annually"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveFirm_ServiceThrows_SetsError()
    {
        var sut = CreateSut();
        _firmService.UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB error"));
        sut.FirmName = "Store";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        Assert.Equal("DB error", sut.ErrorMessage);
        Assert.Empty(sut.SuccessMessage);
    }

    [Fact]
    public void DateFormatPreview_ReflectsSelectedFormat()
    {
        var sut = CreateSut();
        var expected = DateTime.Today.ToString("dd MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

        sut.SelectedDateFormat = "dd MMM yyyy";

        Assert.Equal($"e.g. {expected}", sut.DateFormatPreview);
    }

    [Fact]
    public async Task SaveFirm_InvalidGstinChecksum_BlocksSave()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.State = "Maharashtra";
        sut.GSTNumber = "27AAPFU0939F1ZX";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        Assert.True(sut.HasErrors);
        Assert.Contains("check digit", sut.GstinValidationHint, StringComparison.OrdinalIgnoreCase);
        await _firmService.DidNotReceive().UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveFirm_GstinStateMismatch_BlocksSave()
    {
        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.State = "Delhi";
        sut.GSTNumber = "27AAPFU0939F1ZV";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        Assert.True(sut.HasErrors);
        Assert.Contains("selected state", sut.GstinValidationHint, StringComparison.OrdinalIgnoreCase);
        await _firmService.DidNotReceive().UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveFirm_CompositionRate_WithDecimalComma_PersistsParsedRate()
    {
        _firmService.UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _eventBus.PublishAsync(Arg.Any<FirmUpdatedEvent>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.State = "Maharashtra";
        sut.SelectedGstRegistrationType = "Composition";
        sut.CompositionRate = "1,5";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        await _firmService.Received(1).UpdateFirmAsync(Arg.Is<FirmUpdateDto>(dto =>
            dto.GstRegistrationType == "Composition"
            && dto.CompositionSchemeRate == 1.5m), Arg.Any<CancellationToken>());
        Assert.Empty(sut.ErrorMessage);
    }

    [Fact]
    public async Task SaveFirm_NonComposition_StoresZeroCompositionRate()
    {
        _firmService.UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _eventBus.PublishAsync(Arg.Any<FirmUpdatedEvent>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        sut.FirmName = "Store";
        sut.SelectedGstRegistrationType = "Regular";
        sut.CompositionRate = "12.5";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        await _firmService.Received(1).UpdateFirmAsync(Arg.Is<FirmUpdateDto>(dto =>
            dto.GstRegistrationType == "Regular"
            && dto.CompositionSchemeRate == 0m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadFirm_InFlight_ExposesWorkingState()
    {
        var pendingLoad = new TaskCompletionSource<FirmManagementSnapshot?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _firmService.GetFirmAsync(Arg.Any<CancellationToken>())
            .Returns(_ => pendingLoad.Task);

        var sut = CreateSut();

        var loadTask = sut.LoadFirmCommand.ExecuteAsync(null);
        await Task.Yield();

        Assert.True(sut.IsLoading);
        Assert.True(sut.IsWorking);
        Assert.Equal("Loading business settings...", sut.WorkingMessage);

        pendingLoad.SetResult(new FirmManagementSnapshot(
            FirmName: "Store",
            Address: string.Empty,
            State: "Rajasthan",
            Pincode: string.Empty,
            Phone: string.Empty,
            Email: string.Empty,
            GSTNumber: null,
            PANNumber: null,
            GstRegistrationType: "Regular",
            CompositionSchemeRate: 0m,
            StateCode: "08",
            FinancialYearStartMonth: 4,
            FinancialYearEndMonth: 3,
            CurrencySymbol: "\u20B9",
            DateFormat: "dd/MM/yyyy",
            NumberFormat: "Indian",
            DefaultTaxMode: "Exclusive",
            RoundingMethod: "None",
            NegativeStockAllowed: false,
            NumberToWordsLanguage: "English",
            InvoicePrefix: "INV",
            ReceiptFooterText: "Thank you! Visit again!",
            LogoPath: string.Empty,
            BankName: string.Empty,
            BankAccountNumber: string.Empty,
            BankIFSC: string.Empty,
            ReceiptHeaderText: string.Empty,
            InvoiceResetPeriod: "Never"));

        await loadTask;

        Assert.False(sut.IsWorking);
    }

    [Fact]
    public async Task IsDirty_LoadEditSave_TracksChanges()
    {
        _firmService.GetFirmAsync(Arg.Any<CancellationToken>()).Returns(new FirmManagementSnapshot(
            FirmName: "Store",
            Address: string.Empty,
            State: "Rajasthan",
            Pincode: string.Empty,
            Phone: "9876543210",
            Email: string.Empty,
            GSTNumber: null,
            PANNumber: null,
            GstRegistrationType: "Regular",
            CompositionSchemeRate: 0m,
            StateCode: "08",
            FinancialYearStartMonth: 4,
            FinancialYearEndMonth: 3,
            CurrencySymbol: "\u20B9",
            DateFormat: "dd/MM/yyyy",
            NumberFormat: "Indian",
            DefaultTaxMode: "Exclusive",
            RoundingMethod: "None",
            NegativeStockAllowed: false,
            NumberToWordsLanguage: "English",
            InvoicePrefix: "INV",
            ReceiptFooterText: "Thank you! Visit again!",
            LogoPath: string.Empty,
            BankName: string.Empty,
            BankAccountNumber: string.Empty,
            BankIFSC: string.Empty,
            ReceiptHeaderText: string.Empty,
            InvoiceResetPeriod: "Never"));
        _firmService.UpdateFirmAsync(Arg.Any<FirmUpdateDto>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _eventBus.PublishAsync(Arg.Any<FirmUpdatedEvent>())
            .Returns(Task.CompletedTask);

        var sut = CreateSut();

        await sut.LoadFirmCommand.ExecuteAsync(null);
        Assert.False(sut.IsDirty);

        sut.Phone = "9123456789";
        Assert.True(sut.IsDirty);

        await sut.SaveFirmCommand.ExecuteAsync(null);

        Assert.False(sut.IsDirty);
    }
}
