using AuHub.Shared.Results;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Application.Repositories;

namespace Payment.Application.Commands.ChargeWinner;

public class ChargeWinnerCommandHandler
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;

    public ChargeWinnerCommandHandler(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<bool>> HandleAsync(ChargeWinnerCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await _walletRepository.GetByUserIdAsync(command.UserId, cancellationToken);
            if (wallet == null)
                return Result.Failure<bool>("Wallet not found", 404);

            wallet.TransferFromFrozen(command.Amount);

            var transaction = Transaction.Create(
                command.UserId,
                TransactionType.Win,
                command.Amount,
                $"Оплата выигранного лота {command.ReferenceId}",
                command.ReferenceId);

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
            return Result.Failure<bool>($"Failed to charge winner: {ex.Message}", 500);
        }
    }
}
