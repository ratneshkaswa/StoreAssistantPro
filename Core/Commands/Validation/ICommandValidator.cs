namespace StoreAssistantPro.Core.Commands.Validation;

/// <summary>
/// Validates a command before it reaches the handler. Each command
/// type can have zero or more validators registered in DI — all are
/// executed, and their errors are aggregated.
/// <para>
/// <b>Design rules:</b>
/// <list type="bullet">
///   <item>Validators are pure — no side effects, no DB calls.
///         They inspect only the command DTO properties.</item>
///   <item>Validators must not throw. Return
///         <see cref="ValidationResult.WithError"/> or
///         <see cref="ValidationResult.WithErrors"/> for failures.</item>
///   <item>Multiple validators per command are allowed (each
///         validates a different aspect). The behavior collects
///         all errors before short-circuiting.</item>
///   <item>Validators have no access to the pipeline delegate —
///         they run strictly <b>before</b> the handler.</item>
/// </list>
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public sealed class CreateOrderValidator
///     : ICommandValidator&lt;CreateOrderCommand&gt;
/// {
///     public Task&lt;ValidationResult&gt; ValidateAsync(
///         CreateOrderCommand command, CancellationToken ct)
///     {
///         var errors = new List&lt;ValidationFailure&gt;();
///
///         if (command.Lines.Count == 0)
///             errors.Add(new("Lines", "Order must have at least one line."));
///
///         return Task.FromResult(errors.Count == 0
///             ? ValidationResult.Success()
///             : ValidationResult.WithErrors(errors));
///     }
/// }
/// </code>
/// </para>
/// <para>
/// Registered in DI as:
/// <code>
/// services.AddTransient&lt;ICommandValidator&lt;CreateOrderCommand&gt;,
///                        CreateOrderValidator&gt;();
/// </code>
/// </para>
/// </summary>
/// <typeparam name="TCommand">
/// The command type to validate. Must implement
/// <see cref="ICommand"/> (works with both <see cref="ICommand"/>
/// and <see cref="ICommandRequest{TResult}"/>).
/// </typeparam>
public interface ICommandValidator<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Validates the command and returns a result containing zero or
    /// more <see cref="ValidationFailure"/> instances.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<ValidationResult> ValidateAsync(TCommand command, CancellationToken ct = default);
}
