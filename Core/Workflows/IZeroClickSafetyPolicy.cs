namespace StoreAssistantPro.Core.Workflows;

/// <summary>
/// Pure-logic safety policy that determines which
/// <see cref="ZeroClickActionCategory"/> values are allowed for
/// automatic zero-click execution.
///
/// <para><b>Hardcoded blocklist (immutable — cannot be overridden):</b></para>
/// <list type="table">
///   <listheader>
///     <term>Category</term>
///     <description>Policy</description>
///   </listheader>
///   <item>
///     <term><see cref="ZeroClickActionCategory.Delete"/></term>
///     <description><b>BLOCKED</b> — destructive, irreversible.</description>
///   </item>
///   <item>
///     <term><see cref="ZeroClickActionCategory.SettingsChange"/></term>
///     <description><b>BLOCKED</b> — modifies application configuration.</description>
///   </item>
///   <item>
///     <term><see cref="ZeroClickActionCategory.FinancialConfirmation"/></term>
///     <description><b>BLOCKED</b> — completes financial transactions.</description>
///   </item>
///   <item>
///     <term><see cref="ZeroClickActionCategory.SecuritySensitive"/></term>
///     <description><b>BLOCKED</b> — security/access control changes.</description>
///   </item>
///   <item>
///     <term><see cref="ZeroClickActionCategory.ReadOnly"/></term>
///     <description>Allowed — no side effects.</description>
///   </item>
///   <item>
///     <term><see cref="ZeroClickActionCategory.Navigation"/></term>
///     <description>Allowed — page/focus changes only.</description>
///   </item>
///   <item>
///     <term><see cref="ZeroClickActionCategory.DataEntry"/></term>
///     <description>Allowed — add to cart, auto-fill (reversible).</description>
///   </item>
/// </list>
///
/// <para><b>Architecture:</b></para>
/// <list type="bullet">
///   <item>Registered as a <b>singleton</b>.</item>
///   <item>No WPF types — pure logic, fully unit-testable.</item>
///   <item>Called by <see cref="ZeroClickWorkflowService"/> as an
///         additional safety gate before executing any rule.</item>
///   <item>The hardcoded blocklist cannot be bypassed. Additional
///         categories can be blocked at runtime via
///         <see cref="BlockCategory"/>.</item>
/// </list>
/// </summary>
public interface IZeroClickSafetyPolicy
{
    /// <summary>
    /// Evaluates whether the given rule's action category is allowed
    /// for automatic zero-click execution.
    /// </summary>
    /// <param name="ruleId">The rule being evaluated (for diagnostics).</param>
    /// <param name="category">The action category to check.</param>
    ZeroClickSafetyVerdict Evaluate(string ruleId, ZeroClickActionCategory category);

    /// <summary>
    /// Returns <c>true</c> if the given category is blocked from
    /// zero-click execution (either hardcoded or runtime-blocked).
    /// </summary>
    bool IsBlocked(ZeroClickActionCategory category);

    /// <summary>
    /// Blocks an additional category at runtime. Cannot unblock
    /// hardcoded categories.
    /// </summary>
    void BlockCategory(ZeroClickActionCategory category);

    /// <summary>
    /// Removes a runtime block for a category. Has no effect on
    /// hardcoded blocked categories.
    /// </summary>
    void UnblockCategory(ZeroClickActionCategory category);

    /// <summary>
    /// Returns all currently blocked categories (hardcoded + runtime).
    /// </summary>
    IReadOnlySet<ZeroClickActionCategory> BlockedCategories { get; }

    /// <summary>
    /// Returns only the hardcoded blocked categories that can never
    /// be unblocked.
    /// </summary>
    IReadOnlySet<ZeroClickActionCategory> HardcodedBlockedCategories { get; }
}
