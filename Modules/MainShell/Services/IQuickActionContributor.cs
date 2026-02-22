using StoreAssistantPro.Modules.MainShell.Models;

namespace StoreAssistantPro.Modules.MainShell.Services;

/// <summary>
/// Implemented by each module that wants to contribute quick actions
/// to the POS toolbar without coupling to <c>MainViewModel</c>.
/// <para>
/// Register as <c>services.AddSingleton&lt;IQuickActionContributor, MyContributor&gt;()</c>
/// in the module's DI extension method. <c>MainViewModel</c> discovers all
/// registered contributors at startup and calls <see cref="Contribute"/>.
/// </para>
/// </summary>
public interface IQuickActionContributor
{
    /// <summary>
    /// Registers one or more <see cref="QuickAction"/> items into
    /// <paramref name="service"/>.
    /// </summary>
    void Contribute(IQuickActionService service);
}
