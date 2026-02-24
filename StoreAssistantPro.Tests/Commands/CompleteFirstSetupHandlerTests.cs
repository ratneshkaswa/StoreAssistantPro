using NSubstitute;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Tests.Commands;

public class CompleteFirstSetupHandlerTests
{
    private readonly ISetupService _setupService = Substitute.For<ISetupService>();

    private CompleteFirstSetupHandler CreateSut() => new(_setupService);

    [Fact]
    public async Task HandleAsync_Success_ReturnsSuccess()
    {
        var command = new CompleteFirstSetupCommand("Store", "", "", "1234", "5678", "9012", "123456");

        var result = await CreateSut().HandleAsync(command);

        Assert.True(result.Succeeded);
        await _setupService.Received(1).InitializeAppAsync("Store", "", "", "1234", "5678", "9012", "123456");
    }

    [Fact]
    public async Task HandleAsync_ServiceThrows_ReturnsFailure()
    {
        _setupService.InitializeAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>())
            .Returns(Task.FromException(new InvalidOperationException("Already set up")));

        var result = await CreateSut().HandleAsync(
            new CompleteFirstSetupCommand("S", "", "", "1234", "5678", "9012", "123456"));

        Assert.False(result.Succeeded);
        Assert.Equal("Already set up", result.ErrorMessage);
    }
}
