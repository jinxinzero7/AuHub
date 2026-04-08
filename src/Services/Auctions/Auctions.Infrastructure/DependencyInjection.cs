using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Auctions.Domain.Interfaces;
using Auctions.Infrastructure.Data;
using Auctions.Infrastructure.Repositories;

namespace Auctions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuctionsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(AuctionsDbContext).Assembly.FullName)));

        services.AddScoped<ILotRepository, LotRepository>();

        return services;
    }
}
