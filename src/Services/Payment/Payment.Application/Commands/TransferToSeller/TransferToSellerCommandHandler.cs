using AuHub.Shared.Results;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Application.Repositories;
using Payment.Application.Services;

namespace Payment.Application.Commands.TransferToSeller;

public class TransferToSellerCommandHandler
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;

    public TransferToSellerCommandHandler(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<bool>> HandleAsync(TransferToSellerCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            var duplicateResult = await PaymentOperationIdempotency.CheckAsync(
                _transactionRepository,
                command.SellerId,
                TransactionType.Transfer,
                command.Amount,
                command.ReferenceId,
                cancellationToken);
            if (duplicateResult != null)
                return duplicateResult;

            var wallet = await _walletRepository.GetByUserIdAsync(command.SellerId, cancellationToken);
            if (wallet == null)
            {
                wallet = Wallet.Create(command.SellerId);
                await _walletRepository.AddAsync(wallet, cancellationToken);
            }

            wallet.Deposit(command.Amount);

            var transaction = Transaction.Create(
                command.SellerId,
                TransactionType.Transfer,
                command.Amount,
                $"Перевод за проданный лот {command.ReferenceId}",
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
            return Result.Failure<bool>($"Failed to transfer to seller: {ex.Message}", 500);
        }
    }
}
