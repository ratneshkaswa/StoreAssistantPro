using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class WorkspaceViewModelTests
{
    [Fact]
    public void CreateSut_DoesNotThrow()
    {
        var sut = new WorkspaceViewModel();

        Assert.NotNull(sut);
    }
}
