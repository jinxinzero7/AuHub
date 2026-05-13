using Microsoft.Extensions.DependencyInjection;

namespace Auctions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register Command Handlers
        services.AddScoped<Commands.CreateLot.CreateLotCommandHandler>();
        services.AddScoped<Commands.PublishLot.PublishLotCommandHandler>();
        services.AddScoped<Commands.CompleteLot.CompleteLotCommandHandler>();
        services.AddScoped<Commands.CancelLot.CancelLotCommandHandler>();
        services.AddScoped<Commands.PlaceBid.PlaceBidCommandHandler>();

        // Register Query Handlers
        services.AddScoped<Queries.GetLots.GetLotsQueryHandler>();
        services.AddScoped<Queries.GetLotById.GetLotByIdQueryHandler>();
        services.AddScoped<Queries.GetBidsByLot.GetBidsByLotQueryHandler>();

        return services;
    }
}
