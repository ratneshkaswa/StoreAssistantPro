namespace StoreAssistantPro.Core.Commands;

/// <summary>
/// Represents the absence of a meaningful return value — the command
/// pipeline equivalent of <c>void</c>.
/// <para>
/// Used as <c>TResult</c> in <see cref="ICommandRequest{TResult}"/>
/// for commands that perform an action but do not produce a value:
/// <code>
/// public sealed record SaveProductCommand(string Name, decimal Price)
///     : ICommandRequest&lt;Unit&gt;;
/// </code>
/// </para>
/// <para>
/// <b>Why not <c>bool</c>?</b> A <c>bool</c> result implies the caller
/// should inspect the value. <see cref="Unit"/> signals unambiguously
/// that success/failure is conveyed solely through
/// <see cref="CommandResult{TResult}.Succeeded"/> and
/// <see cref="CommandResult{TResult}.ErrorMessage"/>.
/// </para>
/// </summary>
public readonly record struct Unit
{
    /// <summary>Singleton value — use instead of <c>new Unit()</c>.</summary>
    public static readonly Unit Value = default;
}
