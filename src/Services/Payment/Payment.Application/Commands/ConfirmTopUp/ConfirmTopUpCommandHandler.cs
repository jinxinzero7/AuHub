using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
using Payment.Application.Repositories;
using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Application.Commands.ConfirmTopUp;

public class ConfirmTopUpCommandHandler
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;

    public ConfirmTopUpCommandHandler(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<bool>> HandleAsync(ConfirmTopUpCommand command, CancellationToken cancellationToken = default)
    {
        if (command.UserId == Guid.Empty)
            return Result.Failure<bool>("Invalid user ID", 400);

        if (command.OperationId == Guid.Empty)
            return Result.Failure<bool>("Invalid payment operation ID", 400);

        if (command.Amount <= Money.Zero)
            return Result.Failure<bool>("Amount must be positive", 400);

        try
        {
            var existingTransaction = await _transactionRepository.GetByUserIdTypeAndReferenceIdAsync(
                command.UserId,
                TransactionType.Deposit,
                command.OperationId,
                cancellationToken);

            if (existingTransaction != null)
                return Result.Success(false);

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
                $"Confirmed {command.Provider} top-up: {command.Amount}",
                command.OperationId);

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
            return Result.Failure<bool>($"Failed to confirm top-up: {ex.Message}", 500);
        }
    }
}
