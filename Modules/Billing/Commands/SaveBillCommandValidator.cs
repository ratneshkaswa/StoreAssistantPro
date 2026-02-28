using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Core.Commands.Validation;

namespace StoreAssistantPro.Modules.Billing.Commands;

/// <summary>
/// Validates <see cref="SaveBillCommand"/> before the handler runs.
/// <para>
/// Registered in DI as
/// <c>ICommandValidator&lt;SaveBillCommand&gt;</c> — the
/// <see cref="ValidationPipelineBehavior{TCommand,TResult}"/>
/// resolves and executes it automatically.
/// </para>
/// </summary>
public sealed class SaveBillCommandValidator : ICommandValidator<SaveBillCommand>
{
    public Task<ValidationResult> ValidateAsync(
        SaveBillCommand command, CancellationToken ct = default)
    {
        var errors = new List<ValidationFailure>();

        if (command.IdempotencyKey == Guid.Empty)
            errors.Add(new(nameof(command.IdempotencyKey),
                "Idempotency key is required."));

        if (string.IsNullOrWhiteSpace(command.PaymentMethod))
            errors.Add(new(nameof(command.PaymentMethod),
                "Payment method is required."));

        if (command.Items is not { Count: > 0 })
            errors.Add(new(nameof(command.Items),
                "Bill must contain at least one item."));
        else
        {
            for (var i = 0; i < command.Items.Count; i++)
            {
                var item = command.Items[i];

                if (item.Quantity <= 0)
                    errors.Add(new($"Items[{i}].Quantity",
                        $"Item at position {i} must have a positive quantity."));

                if (item.UnitPrice < 0)
                    errors.Add(new($"Items[{i}].UnitPrice",
                        $"Item at position {i} must have a non-negative price."));
            }
        }

        return Task.FromResult(errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.WithErrors(errors));
    }
}
