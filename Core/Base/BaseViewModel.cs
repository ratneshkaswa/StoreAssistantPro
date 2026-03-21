using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace StoreAssistantPro.Core;

/// <summary>
/// Base class for all application ViewModels. Provides standardized
/// infrastructure for busy-state tracking, error display, validation,
/// and a discoverable title.
/// <para>
/// Extends <see cref="ObservableValidator"/> (which itself extends
/// <see cref="ObservableObject"/>), giving every ViewModel built-in
/// <see cref="INotifyDataErrorInfo"/> support for inline per-field
/// validation.  The existing <see cref="Validate"/> helper for
/// form-level validation continues to work alongside it.
/// </para>
/// <para>
/// <b>Architecture rule:</b> Every ViewModel in every module must derive
/// from <see cref="BaseViewModel"/> to guarantee consistent behavior
/// and make cross-cutting concerns (logging, error handling) easy to add.
/// </para>
/// </summary>
public abstract partial class BaseViewModel : ObservableValidator, IDisposable
{
    private static ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Called once at app startup to wire the DI logger factory into the
    /// base class. All <see cref="RunAsync"/> / <see cref="RunLoadAsync"/>
    /// exception catches will log through this factory.
    /// </summary>
    public static void SetLoggerFactory(ILoggerFactory loggerFactory) =>
        _loggerFactory = loggerFactory;

    /// <summary>
    /// Lazy per-type logger resolved from the static factory.
    /// Returns a no-op logger if the factory was never set.
    /// </summary>
    private ILogger? _vmLogger;
    private ILogger VmLogger => _vmLogger ??=
        _loggerFactory?.CreateLogger(GetType())
        ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance.CreateLogger(GetType());

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
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Identifies the field that caused the first validation failure.
    /// Views use this key for structured focus routing instead of
    /// parsing <see cref="ErrorMessage"/> text.
    /// </summary>
    [ObservableProperty]
    public partial string FirstErrorFieldKey { get; set; } = string.Empty;

    /// <summary>
    /// Success/confirmation message to display in the View after a
    /// successful action (save, delete, PIN change, etc.).
    /// Typically auto-dismissed by <c>AutoDismiss</c> in the template.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    public partial string SuccessMessage { get; set; } = string.Empty;

    /// <summary>
    /// <c>true</c> when <see cref="ErrorMessage"/> is non-empty.
    /// Useful for triggering shake animations or error indicators in the View.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// <c>true</c> when <see cref="SuccessMessage"/> is non-empty.
    /// </summary>
    public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

    /// <summary>
    /// All validation errors from the last <see cref="Validate"/> call (#432).
    /// Views can bind an ItemsControl to this for a validation summary panel.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationErrors))]
    public partial IReadOnlyList<string> ValidationErrors { get; set; } = [];

    /// <summary>
    /// <c>true</c> when <see cref="ValidationErrors"/> contains at least one error.
    /// </summary>
    public bool HasValidationErrors => ValidationErrors.Count > 0;

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
        ValidationErrors = builder.AllErrors;

        ErrorMessage = error ?? string.Empty;
        var nextErrorKey = builder.FirstErrorKey ?? string.Empty;
        if (!string.IsNullOrEmpty(nextErrorKey)
            && string.Equals(FirstErrorFieldKey, nextErrorKey, StringComparison.Ordinal))
        {
            FirstErrorFieldKey = string.Empty;
        }

        FirstErrorFieldKey = nextErrorKey;
        return error is null;
    }

    /// <summary>
    /// Fluent builder for chaining validation rules.
    /// Short-circuits on the first failure.
    /// </summary>
    protected sealed class ValidationBuilder
    {
        private readonly List<string> _errors = [];
        internal string? FirstError => _errors.Count > 0 ? _errors[0] : null;
        internal string? FirstErrorKey { get; private set; }
        internal IReadOnlyList<string> AllErrors => _errors;

        /// <summary>
        /// Add a validation rule. If <paramref name="condition"/> is
        /// <c>false</c>, record the error. The first failure's optional
        /// field key is used for structured focus routing.
        /// </summary>
        public ValidationBuilder Rule(bool condition, string errorMessage, string? fieldKey = null)
        {
            if (!condition)
            {
                _errors.Add(errorMessage);
                if (_errors.Count == 1)
                    FirstErrorKey = fieldKey;
            }
            return this;
        }
    }

    // ── Async helpers ──

    private CancellationTokenSource? _cts;

    /// <summary>
    /// Creates a new <see cref="CancellationTokenSource"/>, cancelling
    /// any previously active one.  Call at the start of every async
    /// operation so navigating away or re-loading cancels stale work.
    /// </summary>
    private CancellationToken ResetCancellation()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        return _cts.Token;
    }

    /// <summary>
    /// Wraps an async action with <see cref="IsBusy"/> management,
    /// error capture, cancellation, and re-entrancy protection.
    /// If the ViewModel is already busy, the call is silently ignored
    /// — this prevents double-click / double-Enter from firing twice.
    /// <para>The <see cref="CancellationToken"/> is passed to the
    /// action; services should forward it to EF / HTTP calls.</para>
    /// <para><b>Usage (mutating operations — save, delete, submit):</b></para>
    /// <code>
    /// [RelayCommand]
    /// private Task SaveAsync() => RunAsync(async ct =>
    /// {
    ///     await service.SaveAsync(item, ct);
    /// });
    /// </code>
    /// </summary>
    protected async Task RunAsync(Func<CancellationToken, Task> action)
    {
        if (IsBusy) return;

        var ct = ResetCancellation();
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await action(ct);
        }
        catch (OperationCanceledException)
        {
            // Silently swallow — user navigated away or re-triggered.
        }
        catch (Exception ex)
        {
            VmLogger.LogError(ex, "RunAsync failed in {ViewModel}", GetType().Name);
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Same as <see cref="RunAsync(Func{CancellationToken, Task})"/>
    /// but returns a result of <typeparamref name="T"/>, or
    /// <c>default</c> on failure or cancellation.
    /// </summary>
    protected async Task<T?> RunAsync<T>(Func<CancellationToken, Task<T>> action)
    {
        if (IsBusy) return default;

        var ct = ResetCancellation();
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            return await action(ct);
        }
        catch (OperationCanceledException)
        {
            return default;
        }
        catch (Exception ex)
        {
            VmLogger.LogError(ex, "RunAsync<T> failed in {ViewModel}", GetType().Name);
            ErrorMessage = ex.Message;
            return default;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Wraps an async data-loading action with <see cref="IsLoading"/>
    /// management, error capture, cancellation, and re-entrancy
    /// protection.  Calling again while already loading cancels the
    /// previous load (e.g. rapid filter changes, page re-entry).
    /// <para><b>Usage (data loading — list, refresh, initial load):</b></para>
    /// <code>
    /// [RelayCommand]
    /// private Task LoadItemsAsync() => RunLoadAsync(async ct =>
    /// {
    ///     Items = await service.GetAllAsync(ct);
    /// });
    /// </code>
    /// </summary>
    protected async Task RunLoadAsync(Func<CancellationToken, Task> action)
    {
        var ct = ResetCancellation();
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            await action(ct);
        }
        catch (OperationCanceledException)
        {
            // Silently swallow — superseded by a newer load.
        }
        catch (Exception ex)
        {
            VmLogger.LogError(ex, "RunLoadAsync failed in {ViewModel}", GetType().Name);
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Resets transient UI state. Override in derived ViewModels to
    /// clear additional form fields.
    /// </summary>
    public virtual void ClearState()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        IsBusy = false;
        IsLoading = false;
    }

    /// <summary>
    /// Disposes the internal <see cref="CancellationTokenSource"/>.
    /// Override in derived ViewModels to unsubscribe from events;
    /// always call <c>base.Dispose()</c>.
    /// </summary>
    public virtual void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        GC.SuppressFinalize(this);
    }
}
