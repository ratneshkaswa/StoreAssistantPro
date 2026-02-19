using CommunityToolkit.Mvvm.ComponentModel;

namespace StoreAssistantPro.Core;

/// <summary>
/// Base class for all application ViewModels. Provides standardized
/// infrastructure for busy-state tracking, error display, and a
/// discoverable title.
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
    /// <see cref="RunAsync"/> on failure, or manually by the ViewModel.
    /// </summary>
    [ObservableProperty]
    public partial string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable title derived from the class name by default.
    /// Override in derived ViewModels for a custom display title.
    /// </summary>
    public virtual string Title => GetType().Name.Replace("ViewModel", "");

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
