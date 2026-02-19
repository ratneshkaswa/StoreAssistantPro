using NSubstitute;
using StoreAssistantPro.Modules.SystemSettings.Commands;
using StoreAssistantPro.Modules.SystemSettings.Services;

namespace StoreAssistantPro.Tests.Commands;

public class ChangeMasterPinHandlerTests
{
    private readonly ISystemSettingsService _settingsService = Substitute.For<ISystemSettingsService>();

    private ChangeMasterPinHandler CreateSut() => new(_settingsService);

    [Fact]
    public async Task HandleAsync_Success_ReturnsSuccess()
    {
        var result = await CreateSut().HandleAsync(new ChangeMasterPinCommand("123456", "654321"));

        Assert.True(result.Succeeded);
        await _settingsService.Received(1).ChangeMasterPinAsync("123456", "654321");
    }

    [Fact]
    public async Task HandleAsync_InvalidCurrent_ReturnsFailure()
    {
        _settingsService.ChangeMasterPinAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("Current password is incorrect.")));

        var result = await CreateSut().HandleAsync(new ChangeMasterPinCommand("000000", "654321"));

        Assert.False(result.Succeeded);
        Assert.Equal("Current password is incorrect.", result.ErrorMessage);
    }
}
