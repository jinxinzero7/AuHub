using AuHub.Shared.Results;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Application.Repositories;

namespace Payment.Application.Commands.ReserveFunds;

public class ReserveFundsCommandHandler
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;

    public ReserveFundsCommandHandler(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<bool>> HandleAsync(ReserveFundsCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var wallet = await _walletRepository.GetByUserIdAsync(command.UserId, cancellationToken);
            if (wallet == null)
            {
                return Result.Failure<bool>("Wallet not found. Please top up your balance first.", 404);
            }

            wallet.Freeze(command.Amount);

            var transaction = Transaction.Create(
                command.UserId,
                TransactionType.Reserve,
                command.Amount,
                $"Резервирование средств для лота {command.ReferenceId}",
                command.ReferenceId);

            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _walletRepository.SaveChangesAsync(cancellationToken);

            return Result.Success(true);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<bool>(ex.Message.Replace("freeze", "").Replace("frozen", ""), 400);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>($"Failed to reserve funds: {ex.Message}", 500);
        }
    }
}
