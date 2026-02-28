using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace StoreAssistantPro.Data;

/// <summary>
/// Design-time factory used by EF Core tools (<c>dotnet ef migrations add</c>).
/// Reads the connection string from <c>appsettings.json</c> so the tools can
/// create a DbContext without starting the WPF host.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .Options;

        return new AppDbContext(options);
    }
}
