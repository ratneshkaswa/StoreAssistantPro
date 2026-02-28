using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Models;

namespace StoreAssistantPro.Modules.Customers.Commands;

public sealed record SaveCustomerCommand(Customer Customer) : ICommandRequest<Unit>;
public sealed record DeleteCustomerCommand(int CustomerId) : ICommandRequest<Unit>;
