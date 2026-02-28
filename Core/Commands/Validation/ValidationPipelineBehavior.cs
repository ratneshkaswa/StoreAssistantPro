using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core.Commands.Validation;

/// <summary>
/// Pipeline behavior that resolves all
/// <see cref="ICommandValidator{TCommand}"/> instances for the
/// current command, executes them, and short-circuits the pipeline
/// with a <see cref="CommandResult{TResult}.Failure"/> if any
/// validation errors are found.
/// <para>
/// <b>Pipeline position:</b> Register this behavior <b>before</b>
/// other behaviors so validation runs first:
/// <code>
/// services.AddTransient(
///     typeof(ICommandPipelineBehavior&lt;,&gt;),
///     typeof(ValidationPipelineBehavior&lt;,&gt;));
/// // then logging, performance, etc.
/// </code>
/// </para>
/// <para>
/// <b>Aggregation:</b> All validators for the command are executed
/// (not short-circuited on first failure). Their errors are merged
/// into a single <see cref="ValidationResult"/> so the caller
/// receives the complete set of validation problems.
/// </para>
/// <para>
/// <b>Zero validators:</b> If no validators are registered for a
/// command type, the behavior calls <c>next()</c> immediately —
/// zero overhead.
/// </para>
/// </summary>
public sealed class ValidationPipelineBehavior<TCommand, TResult>(
    IServiceProvider serviceProvider,
    ILogger<ValidationPipelineBehavior<TCommand, TResult>> logger)
    : ICommandPipelineBehavior<TCommand, TResult>
    where TCommand : ICommandRequest<TResult>
{
    public async Task<CommandResult<TResult>> HandleAsync(
        TCommand command,
        CommandHandlerDelegate<TResult> next,
        CancellationToken ct = default)
    {
        // 1. Resolve all validators for this command type
        var validators = serviceProvider
            .GetServices<ICommandValidator<TCommand>>()
            .ToList();

        if (validators.Count == 0)
            return await next().ConfigureAwait(false);

        // 2. Run all validators and aggregate errors
        var allErrors = new List<ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(command, ct)
                .ConfigureAwait(false);

            if (!result.IsValid)
                allErrors.AddRange(result.Errors);
        }

        // 3. Short-circuit if any errors found
        if (allErrors.Count > 0)
        {
            var aggregated = ValidationResult.WithErrors(allErrors);

            logger.LogWarning(
                "Validation failed for {Command} with {ErrorCount} error(s): {Errors}",
                typeof(TCommand).Name,
                allErrors.Count,
                aggregated.ErrorMessage);

            return CommandResult<TResult>.Failure(aggregated.ErrorMessage);
        }

        // 4. All validators passed — continue pipeline
        return await next().ConfigureAwait(false);
    }
}
