using NSubstitute;
using StoreAssistantPro.Modules.Authentication.Commands;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Tests.Commands;

public class CompleteFirstSetupHandlerTests
{
    private readonly ISetupService _setupService = Substitute.For<ISetupService>();

    private CompleteFirstSetupHandler CreateSut() => new(_setupService);

    private static SetupBusinessOptions DefaultBusinessOptions => new(
        "Regular", 1.0m, null, "Tax-Exclusive", "No Rounding", "English", false, false, null, null);

    [Fact]
    public async Task HandleAsync_Success_ReturnsSuccess()
    {
        var command = new CompleteFirstSetupCommand("Store", "", "", "", "", "", "", "", "INR", "₹", 4, 3, "dd/MM/yyyy", "1234", "5678", "9012", "123456", DefaultBusinessOptions);

        var result = await CreateSut().HandleAsync(command);

        Assert.True(result.Succeeded);
        await _setupService.Received(1).InitializeAppAsync(
            "Store", "", "", "", "", "", "", "", "INR", "₹", 4, 3, "dd/MM/yyyy",
            "1234", "5678", "9012", "123456", Arg.Any<SetupBusinessOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ServiceThrows_ReturnsFailure()
    {
        _setupService.InitializeAppAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SetupBusinessOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Already set up")));

        var result = await CreateSut().HandleAsync(
            new CompleteFirstSetupCommand("S", "", "", "", "", "", "", "", "INR", "₹", 4, 3, "dd/MM/yyyy", "1234", "5678", "9012", "123456", DefaultBusinessOptions));

        Assert.False(result.Succeeded);
        Assert.Equal("Already set up", result.ErrorMessage);
    }

    [Fact]
    public async Task HandleAsync_ForwardsNonAprilFYMonth()
    {
        var command = new CompleteFirstSetupCommand(
            "Store", "", "", "", "", "", "", "", "INR", "₹",
            1, 12, "yyyy-MM-dd", "1234", "5678", "9012", "123456", DefaultBusinessOptions);

        await CreateSut().HandleAsync(command);

        await _setupService.Received(1).InitializeAppAsync(
            "Store", "", "", "", "", "", "", "", "INR", "₹",
            1, 12, "yyyy-MM-dd",
            "1234", "5678", "9012", "123456", Arg.Any<SetupBusinessOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ForwardsAllRegionalSettings()
    {
        var command = new CompleteFirstSetupCommand(
            "Store", "Addr", "Rajasthan", "302001", "9876543210",
            "test@test.com", "08AAAAA0000A1Z5", "ABCDE1234F",
            "INR", "Rs.", 7, 6, "d MMM yyyy",
            "2847", "3916", "5023", "847291", DefaultBusinessOptions);

        await CreateSut().HandleAsync(command);

        await _setupService.Received(1).InitializeAppAsync(
            "Store", "Addr", "Rajasthan", "302001", "9876543210",
            "test@test.com", "08AAAAA0000A1Z5", "ABCDE1234F",
            "INR", "Rs.", 7, 6, "d MMM yyyy",
            "2847", "3916", "5023", "847291", Arg.Any<SetupBusinessOptions>(), Arg.Any<CancellationToken>());
    }
}
