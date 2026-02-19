using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Core.Services;

public class TransactionHelper(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<TransactionHelper> logger) : ITransactionHelper
{
    public async Task ExecuteInTransactionAsync(Func<AppDbContext, Task> operation)
    {
        await ExecuteInTransactionAsync<object?>(async context =>
        {
            await operation(context);
            return null;
        });
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<AppDbContext, Task<TResult>> operation)
    {
        // Obtain the execution strategy from a separate context so the
        // retryable lambda creates a clean context on each attempt.
        await using var strategySource = await contextFactory.CreateDbContextAsync();
        var strategy = strategySource.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var result = await operation(context);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                logger.LogInformation("Transaction committed successfully");
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                logger.LogWarning(ex, "Concurrency conflict during transaction");
                throw new InvalidOperationException(
                    "Data was modified by another user. Please try again.", ex);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed — rolling back");
                throw;
            }
        });
    }
}
