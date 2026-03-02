using NSubstitute;
using StoreAssistantPro.Modules.MainShell.Models;
using StoreAssistantPro.Modules.MainShell.Services;
using StoreAssistantPro.Modules.MainShell.ViewModels;

namespace StoreAssistantPro.Tests.ViewModels;

public class WorkspaceViewModelTests
{
    private readonly IDashboardService _dashboardService = Substitute.For<IDashboardService>();

    private WorkspaceViewModel CreateSut() => new(_dashboardService);

    [Fact]
    public async Task LoadMainWorkspace_CallsDashboardService()
    {
        _dashboardService.GetSummaryAsync(Arg.Any<CancellationToken>()).Returns(DashboardSummary.Empty);

        var sut = CreateSut();
        await sut.LoadMainWorkspaceCommand.ExecuteAsync(null);

        await _dashboardService.Received(1).GetSummaryAsync(Arg.Any<CancellationToken>());
    }
}
