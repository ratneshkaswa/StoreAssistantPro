using StoreAssistantPro.Models;

namespace StoreAssistantPro.Core.Services;

/// <summary>
/// Singleton registry that stores <see cref="FocusMap"/> definitions
/// for all pages, dialogs, and forms in the application.
/// <para>
/// Modules register their maps during DI startup (inside their
/// <c>Add*Module</c> method). At runtime, <see cref="IPredictiveFocusService"/>
/// queries the registry to resolve focus targets for each context.
/// </para>
///
/// <para><b>Registration pattern (module startup):</b></para>
/// <code>
/// var registry = services.GetFocusMapRegistry(); // extension method
/// registry.Register(FocusMap.For("Vendors")
///     .Add("NameInput",    FocusRole.PrimaryInput)
///     .Add("ContactInput", FocusRole.FormField)
///     .Add("SaveButton",   FocusRole.PrimaryAction)
///     .Build());
/// </code>
///
/// <para><b>Query pattern (PredictiveFocusService):</b></para>
/// <code>
/// var map = focusMapRegistry.Get("Vendors");
/// var target = map?.GetLandingTarget() ?? fallback;
/// </code>
///
/// <para><b>Architecture rules:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>Thread-safe — registrations happen during startup, queries
///         happen on the UI thread at runtime.</item>
///   <item>No WPF types — pure data registry.</item>
/// </list>
/// </summary>
public interface IFocusMapRegistry
{
    /// <summary>
    /// Register a <see cref="FocusMap"/> for a context key.
    /// Replaces any existing map for the same key.
    /// </summary>
    /// <param name="map">The focus map to register.</param>
    void Register(FocusMap map);

    /// <summary>
    /// Retrieve the <see cref="FocusMap"/> for the given context key,
    /// or <c>null</c> if none is registered.
    /// </summary>
    /// <param name="contextKey">Page key, dialog key, or form name.</param>
    FocusMap? Get(string contextKey);

    /// <summary>
    /// Returns <c>true</c> if a map is registered for the given key.
    /// </summary>
    bool Contains(string contextKey);

    /// <summary>
    /// Returns all registered context keys.
    /// </summary>
    IReadOnlyCollection<string> GetRegisteredKeys();
}
