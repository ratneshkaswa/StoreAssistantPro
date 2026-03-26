using NSubstitute;
using StoreAssistantPro.Core.Services;
using StoreAssistantPro.Models;
using StoreAssistantPro.Modules.Orders.Services;
using StoreAssistantPro.Modules.Orders.ViewModels;
using StoreAssistantPro.Tests.Helpers;

namespace StoreAssistantPro.Tests.ViewModels;

[Collection("UserPreferences")]
public sealed class OrderManagementViewModelStateTests : IDisposable
{
    private readonly IOrderService _orderService = Substitute.For<IOrderService>();

    public OrderManagementViewModelStateTests() => UserPreferencesStore.ClearForTests();

    public void Dispose() => UserPreferencesStore.ClearForTests();

    [Fact]
    public async Task LoadCommand_Should_Restore_Search_And_Active_Filter()
    {
        UserPreferencesStore.SetOrderManagementState(new SearchFilterViewState
        {
            SearchText = "Sharma",
            ActiveFilter = "Active"
        });

        _orderService.GetStatsAsync(Arg.Any<CancellationToken>())
            .Returns(new OrderStats(1, 2, 1, 3));
        _orderService.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Order>>([
                new Order
                {
                    Id = 1,
                    CustomerName = "Sharma",
                    ItemDescription = "Kurta",
                    Status = "Confirmed",
                    Date = DateTime.Today
                },
                new Order
                {
                    Id = 2,
                    CustomerName = "Sharma",
                    ItemDescription = "Suit",
                    Status = "Ready",
                    Date = DateTime.Today
                },
                new Order
                {
                    Id = 3,
                    CustomerName = "Sharma",
                    ItemDescription = "Saree",
                    Status = "Delivered",
                    Date = DateTime.Today
                }
            ]));

        var sut = new OrderManagementViewModel(_orderService);

        await sut.LoadCommand.ExecuteAsync(null);

        Assert.Equal("Sharma", sut.SearchText);
        Assert.Equal("Active", sut.ActiveStatusFilter);
        Assert.Equal(2, sut.Orders.Count);
        Assert.All(sut.Orders, order => Assert.True(order.Status is not "Delivered" and not "Cancelled"));
    }
}
