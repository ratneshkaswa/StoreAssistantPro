namespace StoreAssistantPro.Models;

/// <summary>
/// The role a <see cref="FocusMapEntry"/> plays in a workflow.
/// Consumers can filter entries by role to find specific landing targets.
/// </summary>
public enum FocusRole
{
    /// <summary>Primary input field — the default landing target.</summary>
    PrimaryInput,

    /// <summary>Search / filter box — landing target on page navigation.</summary>
    SearchInput,

    /// <summary>Regular form field — part of the sequential tab order.</summary>
    FormField,

    /// <summary>Primary action button — Save, Submit, Confirm.</summary>
    PrimaryAction,

    /// <summary>Secondary action button — Cancel, Reset, Back.</summary>
    SecondaryAction,

    /// <summary>Data grid or list — the main data display element.</summary>
    DataGrid
}
