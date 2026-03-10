using StoreAssistantPro.Core.Commands;
using StoreAssistantPro.Modules.Authentication.Services;

namespace StoreAssistantPro.Modules.Authentication.Commands;

public class CompleteFirstSetupHandler(ISetupService setupService)
    : ICommandRequestHandler<CompleteFirstSetupCommand, Unit>
{
    public async Task<CommandResult<Unit>> HandleAsync(CompleteFirstSetupCommand command, CancellationToken ct = default)
    {
        try
        {
            await setupService.InitializeAppAsync(command, ct);
            return CommandResult<Unit>.Success(Unit.Value);
        }
        catch (InvalidOperationException ex)
        {
            return CommandResult<Unit>.Failure(ex.Message);
        }
        catch (Exception)
        {
            return CommandResult<Unit>.Failure("Setup failed. Please try again.");
        }
    }
}
