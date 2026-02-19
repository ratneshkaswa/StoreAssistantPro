using System.Data.Common;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StoreAssistantPro.Data;

namespace StoreAssistantPro.Core.Services;

public class ApplicationInfoService(
    IDbContextFactory<AppDbContext> contextFactory,
    IConfiguration configuration) : IApplicationInfoService
{
    public string AppVersion { get; } =
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";

    public string DotNetVersion { get; } =
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    public string Environment { get; } =
#if DEBUG
        "Development";
#else
        "Production";
#endif

    public string DatabaseServer { get; } = ParseConnectionString(configuration, "Data Source")
                                            ?? ParseConnectionString(configuration, "Server")
                                            ?? "Unknown";

    public string DatabaseName { get; } = ParseConnectionString(configuration, "Initial Catalog")
                                          ?? ParseConnectionString(configuration, "Database")
                                          ?? "Unknown";

    public string LogDirectory { get; } = Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
        "StoreAssistantPro", "Logs");

    public async Task<bool> IsDatabaseConnectedAsync()
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            return await context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<string>> GetPendingMigrationsAsync()
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var pending = await context.Database.GetPendingMigrationsAsync();
            return pending.ToList().AsReadOnly();
        }
        catch
        {
            return [];
        }
    }

    private static string? ParseConnectionString(IConfiguration configuration, string keyword)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString)) return null;

        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        return builder.TryGetValue(keyword, out var value) ? value.ToString() : null;
    }
}
