using Microsoft.Extensions.DependencyInjection;
using Payment.Application.Commands.TopUpWallet;
using Payment.Application.Commands.ReserveFunds;
using Payment.Application.Commands.ReleaseFunds;
using Payment.Application.Commands.ChargeWinner;
using Payment.Application.Commands.TransferToSeller;
using Payment.Application.Commands.RefundFunds;
using Payment.Application.Queries.GetBalance;
using Payment.Application.Queries.GetTransactionHistory;

namespace Payment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<TopUpWalletCommandHandler>();
        services.AddScoped<ReserveFundsCommandHandler>();
        services.AddScoped<ReleaseFundsCommandHandler>();
        services.AddScoped<ChargeWinnerCommandHandler>();
        services.AddScoped<TransferToSellerCommandHandler>();
        services.AddScoped<RefundFundsCommandHandler>();
        services.AddScoped<GetBalanceQueryHandler>();
        services.AddScoped<GetTransactionHistoryQueryHandler>();

        return services;
    }
}
