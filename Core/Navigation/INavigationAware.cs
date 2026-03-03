namespace StoreAssistantPro.Core.Navigation;

/// <summary>
/// Optional lifecycle interface for ViewModels hosted inside the
/// navigation content region. When <see cref="NavigationService"/>
/// navigates to a ViewModel that implements this interface, it calls
/// <see cref="OnNavigatedTo"/>; when navigating away, it calls
/// <see cref="OnNavigatedFrom"/> on the outgoing ViewModel.
/// <para>
/// <b>Architecture rule:</b> Only implement this when a page needs
/// to react to navigation events (e.g., refresh data, cancel
/// background work, release resources). Simple pages that load
/// data via <c>[RelayCommand] LoadAsync</c> triggered from the View's
/// <c>Loaded</c> event do not need this.
/// </para>
///
/// <para><b>Typical usage:</b></para>
/// <code>
/// public partial class ProductsViewModel : BaseViewModel, INavigationAware
/// {
///     public Task OnNavigatedTo(CancellationToken ct) => LoadProductsAsync();
///     public void OnNavigatedFrom() { }
/// }
/// </code>
/// </summary>
public interface INavigationAware
{
    /// <summary>
    /// Called after the ViewModel is set as the active view.
    /// Use for initial data loading, timer starts, or event subscriptions.
    /// </summary>
    /// <param name="ct">
    /// Cancellation token that fires when the user navigates away
    /// before this method completes.
    /// </param>
    Task OnNavigatedTo(CancellationToken ct = default);

    /// <summary>
    /// Called when the ViewModel is being replaced by another view.
    /// Use for cancelling background work, unsubscribing from events,
    /// or persisting transient state.
    /// </summary>
    void OnNavigatedFrom();
}
