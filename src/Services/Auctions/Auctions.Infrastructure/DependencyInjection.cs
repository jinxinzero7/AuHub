using Auctions.Domain.Interfaces;
using Auctions.Infrastructure.BackgroundServices;
using Auctions.Infrastructure.Data;
using Auctions.Infrastructure.Events;
using Auctions.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auctions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext
        services.AddDbContext<AuctionsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Register Repositories
        services.AddScoped<ILotRepository, LotRepository>();
        services.AddScoped<IBidRepository, BidRepository>();
        services.AddScoped<ILotImageRepository, LotImageRepository>();

        // Register Outbox
        services.AddScoped<IOutbox, Outbox>();

        // Register Domain Event Dispatcher
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Register Background Services
        services.AddHostedService<AuctionCompletionService>();
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
