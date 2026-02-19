using StoreAssistantPro.Core.Workflows;

namespace StoreAssistantPro.Modules.Billing.Workflows;

/// <summary>
/// Future billing workflow placeholder.
/// <para>
/// Planned steps:
/// <list type="bullet">
///   <item><b>NewBill</b>     — initialize a new billing session.</item>
///   <item><b>AddItems</b>    — scan/search products, add to cart.</item>
///   <item><b>Payment</b>     — select payment method, process.</item>
///   <item><b>Receipt</b>     — generate and optionally print receipt.</item>
///   <item><b>Reset</b>       — clear session, ready for next customer.</item>
/// </list>
/// </para>
/// </summary>
public class BillingWorkflow : IWorkflow
{
    public const string WorkflowName = "Billing";

    private static readonly WorkflowStep NewBill = new("NewBill", "Billing");
    private static readonly WorkflowStep AddItems = new("AddItems", "Billing");
    private static readonly WorkflowStep Payment = new("Payment", "Billing");
    private static readonly WorkflowStep Receipt = new("Receipt", "Billing");
    private static readonly WorkflowStep Reset = new("Reset");

    public string Name => WorkflowName;

    public IReadOnlyList<WorkflowStep> Steps { get; } =
        [NewBill, AddItems, Payment, Receipt, Reset];

    public Task<StepResult> ExecuteStepAsync(WorkflowStep step, WorkflowContext context)
    {
        // TODO: Implement when billing module is built.
        // Each step will interact with IBillingService and update WorkflowContext.
        return Task.FromResult(StepResult.Continue);
    }

    public Task OnCompletedAsync(WorkflowContext context) => Task.CompletedTask;

    public Task OnCancelledAsync(WorkflowContext context) => Task.CompletedTask;
}
