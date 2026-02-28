using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Sales.Commands;

public sealed record ProcessReturnCommand(
    int SaleId,
    int SaleItemId,
    int Quantity,
    decimal RefundAmount,
    string Reason,
    bool RestoreStock) : ICommandRequest<Unit>;
