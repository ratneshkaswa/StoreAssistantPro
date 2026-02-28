using StoreAssistantPro.Core.Session;
using StoreAssistantPro.Core.Workflows;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Modules.Authentication.Workflows;

/// <summary>
/// Controls the login sequence:
/// 1. Show user selection + PIN entry (via AuthenticationFlow)
/// 2. Create session
/// Outcome: user is authenticated and session is active.
/// </summary>
public class LoginWorkflow(
    IAuthenticationFlow authFlow,
    ISessionService sessionService) : IWorkflow
{
    public const string WorkflowName = "Login";

    private static readonly WorkflowStep SelectAndAuthenticate = new("SelectAndAuthenticate");
    private static readonly WorkflowStep CreateSession = new("CreateSession");

    public string Name => WorkflowName;

    public IReadOnlyList<WorkflowStep> Steps { get; } =
        [SelectAndAuthenticate, CreateSession];

    public async Task<StepResult> ExecuteStepAsync(WorkflowStep step, WorkflowContext context)
    {
        return step.Key switch
        {
            "SelectAndAuthenticate" => Authenticate(context),
            "CreateSession" => await CreateSessionAsync(context),
            _ => StepResult.Continue
        };
    }

    private StepResult Authenticate(WorkflowContext context)
    {
        if (!authFlow.TryLogin(out var userType))
            return StepResult.Cancel;

        context.Set("UserType", userType);
        return StepResult.Continue;
    }

    private async Task<StepResult> CreateSessionAsync(WorkflowContext context)
    {
        var userType = context.Get<Models.UserType>("UserType");
        await sessionService.LoginAsync(userType);
        return StepResult.Complete;
    }

    public Task OnCompletedAsync(WorkflowContext context) => Task.CompletedTask;

    public Task OnCancelledAsync(WorkflowContext context) => Task.CompletedTask;
}
