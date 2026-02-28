namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Result returned by every command handler. Carries success/failure
/// plus an optional error message so ViewModels can update UI state
/// without catching exceptions from business logic.
/// </summary>
public sealed record CommandResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static CommandResult Success() => new() { Succeeded = true };
    public static CommandResult Failure(string error) => new() { Succeeded = false, ErrorMessage = error };
}

/// <summary>
/// Typed result returned by <see cref="ICommandRequestHandler{TCommand,TResult}"/>
/// handlers. Extends the base <see cref="CommandResult"/> pattern with a
/// strongly-typed <see cref="Value"/> on success.
/// <para>
/// <b>Usage:</b>
/// <code>
/// // Handler returns:
/// return CommandResult&lt;int&gt;.Success(newOrder.Id);
///
/// // Caller inspects:
/// if (result.Succeeded)
///     var orderId = result.Value;
/// </code>
/// </para>
/// </summary>
/// <typeparam name="TResult">Type of the value produced on success.</typeparam>
public sealed record CommandResult<TResult>
{
    public bool Succeeded { get; private init; }
    public TResult? Value { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static CommandResult<TResult> Success(TResult value) => new()
    {
        Succeeded = true,
        Value = value
    };

    public static CommandResult<TResult> Failure(string error) => new()
    {
        Succeeded = false,
        ErrorMessage = error
    };

    /// <summary>
    /// Converts this typed result to a plain <see cref="CommandResult"/>,
    /// discarding the value. Useful when pipeline behaviors need to
    /// return a generic result.
    /// </summary>
    public CommandResult ToBase() => Succeeded
        ? CommandResult.Success()
        : CommandResult.Failure(ErrorMessage ?? "Unknown error");
}
