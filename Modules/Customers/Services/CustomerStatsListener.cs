using Microsoft.Extensions.Logging;
using StoreAssistantPro.Core.Events;
using StoreAssistantPro.Modules.Customers.Services;
using StoreAssistantPro.Modules.Sales.Events;

namespace StoreAssistantPro.Modules.Customers.Services;

/// <summary>
/// Listens for <see cref="SaleCompletedEvent"/> and updates customer
/// purchase stats (total amount, visit count) when a sale is linked
/// to a customer.
/// </summary>
public class CustomerStatsListener
{
    private readonly IEventBus _eventBus;
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomerStatsListener> _logger;

    public CustomerStatsListener(
        IEventBus eventBus,
        ICustomerService customerService,
        ILogger<CustomerStatsListener> logger)
    {
        _eventBus = eventBus;
        _customerService = customerService;
        _logger = logger;

        _eventBus.Subscribe<SaleCompletedEvent>(OnSaleCompletedAsync);
    }

    private async Task OnSaleCompletedAsync(SaleCompletedEvent e)
    {
        if (e.CustomerId is null) return;

        try
        {
            await _customerService.UpdatePurchaseStatsAsync(e.CustomerId.Value, e.TotalAmount);
            _logger.LogInformation("Updated purchase stats for customer {CustomerId}: +{Amount:C}",
                e.CustomerId.Value, e.TotalAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update customer stats for customer {CustomerId}", e.CustomerId);
        }
    }
}
