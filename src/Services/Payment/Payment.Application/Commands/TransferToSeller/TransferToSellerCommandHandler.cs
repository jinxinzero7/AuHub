using AuHub.Shared.Results;
using AuHub.Shared.ValueObjects;
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

            var wallet = await GetOrCreateWalletAsync(command.SellerId, cancellationToken);

            wallet.Deposit(command.Amount);

            var transaction = Transaction.Create(
                command.SellerId,
                TransactionType.Transfer,
                command.Amount,
                $"Перевод за проданный лот {command.ReferenceId}",
                command.ReferenceId);

            await _transactionRepository.AddAsync(transaction, cancellationToken);

            if (command.ServiceFee > Money.Zero)
            {
                var platformWallet = await GetOrCreateWalletAsync(
                    TransferToSellerCommand.PlatformWalletUserId,
                    cancellationToken);

                platformWallet.Deposit(command.ServiceFee);

                var serviceFeeTransaction = Transaction.Create(
                    TransferToSellerCommand.PlatformWalletUserId,
                    TransactionType.ServiceFee,
                    command.ServiceFee,
                    $"Service fee for lot {command.ReferenceId}",
                    command.ReferenceId);

                await _transactionRepository.AddAsync(serviceFeeTransaction, cancellationToken);
            }

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

    private async Task<Wallet> GetOrCreateWalletAsync(Guid userId, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
        if (wallet != null)
            return wallet;

        wallet = Wallet.Create(userId);
        await _walletRepository.AddAsync(wallet, cancellationToken);
        return wallet;
    }
}
