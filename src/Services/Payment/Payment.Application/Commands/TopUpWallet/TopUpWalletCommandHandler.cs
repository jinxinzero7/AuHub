using AuHub.Shared.Results;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Application.Repositories;

namespace Payment.Application.Commands.TopUpWallet;

public class TopUpWalletCommandHandler
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;

    public TopUpWalletCommandHandler(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<bool>> HandleAsync(TopUpWalletCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await _walletRepository.GetByUserIdAsync(command.UserId, cancellationToken);
            if (wallet == null)
            {
                wallet = Wallet.Create(command.UserId);
                await _walletRepository.AddAsync(wallet, cancellationToken);
            }

            wallet.Deposit(command.Amount);

            var transaction = Transaction.Create(
                command.UserId,
                TransactionType.Deposit,
                command.Amount,
                $"Пополнение баланса: {command.Amount:C}");

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _walletRepository.SaveChangesAsync(cancellationToken);

            return Result.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<bool>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>($"Failed to top up wallet: {ex.Message}", 500);
        }
    }
}
