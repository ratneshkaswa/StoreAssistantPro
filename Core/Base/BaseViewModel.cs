using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Core;

/// <summary>
/// Base class for all application ViewModels. Provides standardized
/// infrastructure for busy-state tracking, error display, validation,
/// and a discoverable title.
/// <para>
/// <b>Architecture rule:</b> Every ViewModel in every module must derive
/// from <see cref="BaseViewModel"/> to guarantee consistent behavior
/// and make cross-cutting concerns (logging, error handling) easy to add.
/// </para>
/// </summary>
public abstract partial class BaseViewModel : ObservableObject
{
    /// <summary>
    /// Indicates whether the ViewModel is performing a long-running operation.
    /// Bind to this in the View to show spinners, disable controls, etc.
    /// </summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// Indicates data is being loaded from a service.
    /// Separate from <see cref="IsBusy"/> so a form can be disabled (IsBusy)
    /// independently from a loading indicator (IsLoading).
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    /// <summary>
    /// Last error message to display in the View. Set by
    /// <see cref="RunAsync"/> on failure, or by <see cref="Validate"/>.
    /// </summary>
    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable title derived from the class name by default.
    /// Override in derived ViewModels for a custom display title.
    /// </summary>
    public virtual string Title => GetType().Name.Replace("ViewModel", "");

    // ── Validation ──

    /// <summary>
    /// Runs a chain of validation rules. Sets <see cref="ErrorMessage"/>
    /// to the first failure and returns <c>false</c>, or clears
    /// <see cref="ErrorMessage"/> and returns <c>true</c> if all pass.
    /// <para>Usage:</para>
    /// <code>
    /// if (!Validate(v => v
    ///     .Rule(InputValidator.IsRequired(Name), "Name is required.")
    ///     .Rule(InputValidator.IsValidUserPin(Pin), "PIN must be 4 digits.")))
    ///     return;
    /// </code>
    /// </summary>
    protected bool Validate(Action<ValidationBuilder> configure)
    {
        var builder = new ValidationBuilder();
        configure(builder);

        var error = builder.FirstError;

        ErrorMessage = error ?? string.Empty;
        return error is null;
    }

    /// <summary>
    /// Fluent builder for chaining validation rules.
    /// Short-circuits on the first failure.
    /// </summary>
    protected sealed class ValidationBuilder
    {
        internal string? FirstError { get; private set; }

        /// <summary>
        /// Add a validation rule. If <paramref name="condition"/> is
        /// <c>false</c> and no prior rule has failed, record the error.
        /// </summary>
        public ValidationBuilder Rule(bool condition, string errorMessage)
        {
            if (FirstError is null && !condition)
                FirstError = errorMessage;
            return this;
        }
    }

    // ── Async helpers ──

    /// <summary>
    /// Convenience helper that wraps an async action with
    /// <see cref="IsBusy"/> management and error capture.
    /// <para>Usage from a <c>[RelayCommand]</c> method:</para>
    /// <code>
    /// [RelayCommand]
    /// private Task SaveAsync() => RunAsync(async () =>
    /// {
    ///     await service.SaveAsync(...);
    /// });
    /// </code>
    /// </summary>
    protected async Task RunAsync(Func<Task> action)
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Same as <see cref="RunAsync(Func{Task})"/> but returns a result
    /// of type <typeparamref name="T"/>, or <c>default</c> on failure.
    /// </summary>
    protected async Task<T?> RunAsync<T>(Func<Task<T>> action)
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Resets transient UI state. Override in derived ViewModels to
    /// clear additional form fields.
    /// </summary>
    public virtual void ClearState()
    {
        ErrorMessage = string.Empty;
        IsBusy = false;
        IsLoading = false;
    }
}
