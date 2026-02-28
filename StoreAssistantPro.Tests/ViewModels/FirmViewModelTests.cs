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
        _firmService.GetFirmAsync().Returns(new AppConfig
        {
            FirmName = "Test Store",
            Address = "123 Main St",
            Phone = "555-0100"
        });

        var sut = CreateSut();
        await sut.LoadFirmCommand.ExecuteAsync(null);

        Assert.Equal("Test Store", sut.FirmName);
        Assert.Equal("123 Main St", sut.Address);
        Assert.Equal("555-0100", sut.Phone);
    }

    [Fact]
    public async Task LoadFirm_NullConfig_DoesNotThrow()
    {
        _firmService.GetFirmAsync().Returns((AppConfig?)null);

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
        await _firmService.DidNotReceive().UpdateFirmAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task SaveFirm_ValidInput_CallsServiceAndShowsSuccess()
    {
        var sut = CreateSut();
        sut.FirmName = "Updated Store";
        sut.Address = "456 Oak Ave";
        sut.Phone = "555-0200";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        await _firmService.Received(1).UpdateFirmAsync("Updated Store", "456 Oak Ave", "555-0200", Arg.Any<string>());
        await _eventBus.Received(1).PublishAsync(Arg.Is<FirmUpdatedEvent>(e =>
            e.FirmName == "Updated Store"));
        Assert.Equal("Firm information saved.", sut.SuccessMessage);
        Assert.Empty(sut.ErrorMessage);
    }

    [Fact]
    public async Task SaveFirm_ServiceThrows_SetsError()
    {
        _firmService.UpdateFirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("DB error")));

        var sut = CreateSut();
        sut.FirmName = "Store";

        await sut.SaveFirmCommand.ExecuteAsync(null);

        Assert.Equal("DB error", sut.ErrorMessage);
        Assert.Empty(sut.SuccessMessage);
    }
}
