using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.FinancialYears.Services;
using StoreAssistantPro.Modules.FinancialYears.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class FinancialYearViewModelTests
{
    private readonly IFinancialYearService _financialYearService = Substitute.For<IFinancialYearService>();
    private readonly IRegionalSettingsService _regionalSettings = Substitute.For<IRegionalSettingsService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();

    private FinancialYearViewModel CreateSut()
    {
        _regionalSettings.Now.Returns(new DateTime(2026, 3, 26));
        return new FinancialYearViewModel(_financialYearService, _regionalSettings, _dialogService);
    }

    [Fact]
    public async Task ShowResetConfirmationCommand_WhenCancelled_ShouldNotReset()
    {
        _dialogService.Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        var sut = CreateSut();

        await sut.ShowResetConfirmationCommand.ExecuteAsync(null);

        _dialogService.Received(1).Confirm(
            Arg.Is<string>(message => message.Contains("Reset billing numbers", StringComparison.Ordinal)),
            "Reset Billing",
            "Reset Billing",
            "Cancel");
        await _financialYearService.DidNotReceive().EnsureCurrentYearAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShowResetConfirmationCommand_WhenConfirmed_ShouldResetAndReload()
    {
        var currentYear = new FinancialYear
        {
            Id = 1,
            Name = "2025-26",
            StartDate = new DateTime(2025, 4, 1),
            EndDate = new DateTime(2026, 3, 31),
            IsCurrent = true
        };

        _dialogService.Confirm(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        _financialYearService.EnsureCurrentYearAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _financialYearService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<FinancialYear>>([currentYear]));
        _financialYearService.GetCurrentAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<FinancialYear?>(currentYear));

        var sut = CreateSut();

        await sut.ShowResetConfirmationCommand.ExecuteAsync(null);

        Assert.Equal("Billing numbers have been reset for the current financial year.", sut.SuccessMessage);
        Assert.Equal(currentYear.Name, sut.CurrentYear?.Name);
        await _financialYearService.Received(1).EnsureCurrentYearAsync(Arg.Any<CancellationToken>());
        await _financialYearService.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await _financialYearService.Received(1).GetCurrentAsync(Arg.Any<CancellationToken>());
    }
}
