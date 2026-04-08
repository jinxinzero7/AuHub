using Microsoft.EntityFrameworkCore;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;
using Auctions.Infrastructure.Data;

namespace Auctions.Infrastructure.Repositories;

public class LotRepository : ILotRepository
{
    private readonly AuctionsDbContext _context;

    public LotRepository(AuctionsDbContext context)
    {
        _context = context;
    }

    public async Task<Lot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Lots
            .Include(l => l.Bids)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<List<Lot>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Lots
            .Include(l => l.Bids)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Lot>> GetActiveLotsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Lots
            .Include(l => l.Bids)
            .Where(l => l.Status == LotStatus.Active)
            .OrderBy(l => l.EndTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        await _context.Lots.AddAsync(lot, cancellationToken);
    }

    public Task UpdateAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        _context.Lots.Update(lot);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
