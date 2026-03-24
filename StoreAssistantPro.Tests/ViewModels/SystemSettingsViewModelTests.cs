using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Authentication.Services;
using StoreAssistantPro.Modules.Settings.Services;
using StoreAssistantPro.Modules.Settings.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class SystemSettingsViewModelTests
{
    private readonly ISystemSettingsService _settingsService = Substitute.For<ISystemSettingsService>();
    private readonly ILoginService _loginService = Substitute.For<ILoginService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly IUiDensityService _uiDensityService = Substitute.For<IUiDensityService>();

    private SystemSettingsViewModel CreateSut() => new(_settingsService, _loginService, _dialogService, _uiDensityService);

    [Fact]
    public async Task LoadCommand_SyncsNumericBridgeProperties()
    {
        _settingsService.GetAsync(Arg.Any<CancellationToken>()).Returns(new SystemSettings
        {
            DefaultPrinter = "Counter Printer",
            PrinterWidth = 58,
            DefaultPageSize = "Thermal",
            AutoLogoutMinutes = 15
        });

        var sut = CreateSut();

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal(58, sut.PrinterWidth);
        Assert.Equal(58d, sut.PrinterWidthValue);
        Assert.Equal("Thermal", sut.DefaultPageSize);
        Assert.Equal(15, sut.AutoLogoutMinutes);
        Assert.Equal(15d, sut.AutoLogoutMinutesValue);
    }

    [Fact]
    public void NumericBridgeProperties_RoundAndClamp_ToBackedIntegers()
    {
        var sut = CreateSut();

        sut.PrinterWidthValue = 79.6;
        sut.AutoLogoutMinutesValue = -5;

        Assert.Equal(80, sut.PrinterWidth);
        Assert.Equal(80d, sut.PrinterWidthValue);
        Assert.Equal(0, sut.AutoLogoutMinutes);
        Assert.Equal(0d, sut.AutoLogoutMinutesValue);
        Assert.Equal("Auto-logout is disabled.", sut.AutoLogoutSummary);
    }

    [Fact]
    public void SetDensityModeCommand_UpdatesUiDensityService()
    {
        var sut = CreateSut();

        sut.SetDensityModeCommand.Execute("Compact");

        Assert.True(sut.IsCompactModeEnabled);
        _uiDensityService.Received(1).SetCompactMode(true);
    }
}
