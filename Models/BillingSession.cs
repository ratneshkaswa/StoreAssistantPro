using System.ComponentModel.DataAnnotations;

namespace StoreAssistantPro.Models;

/// <summary>
/// Persisted billing session entity that survives application restarts.
/// <para>
/// Stores the current cart state as serialized JSON so an operator can
/// resume an in-progress sale after a crash or planned restart.
/// </para>
/// <para>
/// <b>Lifecycle:</b>
/// </para>
/// <list type="bullet">
///   <item>Created when a billing session starts
///         (<see cref="BillingSessionState.Active"/>).</item>
///   <item>Updated on every cart mutation (add/remove item, apply discount).</item>
///   <item>Marked <see cref="IsActive"/> = <c>false</c> when the session
///         completes or is cancelled.</item>
///   <item>On startup, the most recent row with <see cref="IsActive"/> =
///         <c>true</c> is offered for resume.</item>
/// </list>
/// <para>
/// Only one active session per user is expected at any time.
/// </para>
/// </summary>
public class BillingSession
{
    public int Id { get; set; }

    /// <summary>
    /// Correlation identifier shared with the in-memory billing session service
    /// so a persisted row can be matched to the live session.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <summary>
    /// The user who owns this session.
    /// Maps to <see cref="UserCredential.Id"/>.
    /// </summary>
    public int UserId { get; set; }
    public UserCredential? User { get; set; }

    /// <summary>
    /// <c>true</c> while the session is in progress and eligible for
    /// resume. Set to <c>false</c> on completion or cancellation.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// JSON-serialized cart data (line items, quantities, discounts,
    /// payment state). Deserialized on resume.
    /// </summary>
    [Required]
    public string SerializedBillData { get; set; } = "{}";

    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
