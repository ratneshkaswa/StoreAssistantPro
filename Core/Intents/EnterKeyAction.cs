namespace StoreAssistantPro.Core.Intents;

/// <summary>
/// The action <see cref="ISmartEnterKeyService"/> recommends for an
/// Enter keypress.
/// </summary>
public enum EnterKeyAction
{
    /// <summary>
    /// No high-confidence action available.
    /// Proceed with standard field navigation (move to next field)
    /// or execute <c>DefaultCommand</c> if at the last field.
    /// </summary>
    MoveNext,

    /// <summary>
    /// A high-confidence intent has been detected and the associated
    /// action should be auto-executed. The <see cref="EnterKeyDecision.ActionId"/>
    /// identifies which action was triggered.
    /// </summary>
    Execute,

    /// <summary>
    /// The service is busy (e.g., a previous auto-execution is still
    /// in progress). Suppress the Enter press entirely.
    /// </summary>
    Suppress
}
