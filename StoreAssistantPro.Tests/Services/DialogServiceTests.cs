using Microsoft.Extensions.Logging;
using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Modules.MainShell.Services;

namespace StoreAssistantPro.Tests.Services;

public class DialogServiceTests
{
    [Fact]
    public void ShowDialog_WhenRegistryThrows_ReturnsFalse()
    {
        var registry = Substitute.For<IWindowRegistry>();
        registry.ShowDialog("FirmManagement").Returns(_ => throw new InvalidOperationException("boom"));

        var sut = new DialogService(
            registry,
            Substitute.For<IWindowSizingService>(),
            Substitute.For<ILogger<DialogService>>());

        var result = sut.ShowDialog("FirmManagement");

        Assert.False(result);
    }
}
