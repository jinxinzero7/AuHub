using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Payment.Application.Repositories;

namespace Payment.Application.Queries.GetBalance;

public class GetBalanceQueryHandler
{
    private readonly IWalletRepository _walletRepository;

    public GetBalanceQueryHandler(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task<Result<BalanceResponse>> HandleAsync(GetBalanceQuery query, CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await _walletRepository.GetByUserIdAsync(query.UserId, cancellationToken);
            if (wallet == null)
            {
                return Result.Success(new BalanceResponse
                {
                    UserId = query.UserId,
                    Balance = Money.Zero,
                    FrozenBalance = Money.Zero
                });
            }

            return Result.Success(new BalanceResponse
            {
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                FrozenBalance = wallet.FrozenBalance
            });
        }
        catch (Exception ex)
        {
            return Result.Failure<BalanceResponse>($"Failed to get balance: {ex.Message}", 500);
        }
    }
}
