using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Sales.Commands;

public sealed record CompleteSaleCommand(
    decimal TotalAmount,
    string PaymentMethod,
    IReadOnlyList<SaleItemDto> Items) : ICommand;

public sealed record SaleItemDto(
    int ProductId,
    int Quantity,
    decimal UnitPrice);
