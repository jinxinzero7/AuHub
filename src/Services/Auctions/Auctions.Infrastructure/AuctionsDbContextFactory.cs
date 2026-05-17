using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Auctions.Infrastructure.Data;

namespace Auctions.Infrastructure;

public class AuctionsDbContextFactory : IDesignTimeDbContextFactory<AuctionsDbContext>
{
    public AuctionsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Database=auctionsdb;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AuctionsDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AuctionsDbContext(optionsBuilder.Options);
    }
}
