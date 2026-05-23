using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Application.Repositories;
using Payment.Infrastructure.Data;

namespace Payment.Infrastructure.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly PaymentDbContext _context;

    public WalletRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
    {
        await _context.Wallets.AddAsync(wallet, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class TransactionRepository : ITransactionRepository
{
    private readonly PaymentDbContext _context;

    public TransactionRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<List<Transaction>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
