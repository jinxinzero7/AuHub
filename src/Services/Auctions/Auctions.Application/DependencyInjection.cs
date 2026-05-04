using Microsoft.Extensions.DependencyInjection;
using Auctions.Application.Commands.CreateLot;
using Auctions.Application.Commands.PlaceBid;
using Auctions.Application.Commands.PublishLot;
using Auctions.Application.Commands.CompleteLot;
using Auctions.Application.Commands.CancelLot;
using Auctions.Application.Commands.Auth.Register;
using Auctions.Application.Commands.Auth.Login;
using Auctions.Application.Commands.Auth.RefreshToken;
using Auctions.Application.Queries.GetLots;
using Auctions.Application.Queries.GetLotById;
using Auctions.Application.Queries.GetBidsByLot;
using Auctions.Application.Services;

namespace Auctions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IAuthService, AuthService>();

        // Регистрация handlers
        services.AddScoped<CreateLotCommandHandler>();
        services.AddScoped<PlaceBidCommandHandler>();
        services.AddScoped<PublishLotCommandHandler>();
        services.AddScoped<CompleteLotCommandHandler>();
        services.AddScoped<CancelLotCommandHandler>();
        services.AddScoped<RegisterCommandHandler>();
        services.AddScoped<LoginCommandHandler>();
        services.AddScoped<RefreshTokenCommandHandler>();
        services.AddScoped<GetLotsQueryHandler>();
        services.AddScoped<GetLotByIdQueryHandler>();
        services.AddScoped<GetBidsByLotQueryHandler>();

        return services;
    }
}
