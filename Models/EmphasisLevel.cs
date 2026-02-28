namespace StoreAssistantPro.Models;

/// <summary>
/// Visual emphasis level assigned to a <see cref="WorkspaceZone"/>
/// by the Calm UI system.
/// <list type="bullet">
///   <item><see cref="Full"/>     — Normal rendering (opacity 1.0, full scale).</item>
///   <item><see cref="Muted"/>    — Slightly reduced presence (opacity ~0.7, minor scale).</item>
///   <item><see cref="Receded"/>  — Significantly reduced (opacity ~0.45, compact scale).</item>
/// </list>
/// </summary>
public enum EmphasisLevel
{
    /// <summary>Normal rendering — full opacity and scale.</summary>
    Full,

    /// <summary>Slightly de-emphasised — still usable but visually quieter.</summary>
    Muted,

    /// <summary>Strongly de-emphasised — chrome recedes to maximise focus zone.</summary>
    Receded
}
