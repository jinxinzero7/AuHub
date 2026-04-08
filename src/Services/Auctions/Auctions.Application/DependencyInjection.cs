using Microsoft.Extensions.DependencyInjection;
using Auctions.Application.Commands.CreateLot;
using Auctions.Application.Queries.GetLots;

namespace Auctions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Регистрация handlers
        services.AddScoped<CreateLotCommandHandler>();
        services.AddScoped<GetLotsQueryHandler>();

        return services;
    }
}
