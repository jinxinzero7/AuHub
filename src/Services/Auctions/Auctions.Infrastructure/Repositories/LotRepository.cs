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
            .Include(l => l.Images)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Lot>> GetPublicLotsAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Lots
            .Include(l => l.Bids)
            .Include(l => l.Images)
            .Where(l => !l.IsDeleted &&
                       (l.Status == LotStatus.Active ||
                        l.Status == LotStatus.Completed ||
                        l.Status == LotStatus.CompletedNoWinner));

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(term) || l.Description.ToLower().Contains(term));
        }

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Lot>> GetActiveLotsAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Lots
            .Include(l => l.Bids)
            .Include(l => l.Images)
            .Where(l => !l.IsDeleted && l.Status == LotStatus.Active);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(l => l.Title.ToLower().Contains(term) || l.Description.ToLower().Contains(term));
        }

        return await query
            .OrderBy(l => l.EndTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Lot>> GetBySellerIdAsync(Guid sellerId, bool includeDrafts, CancellationToken cancellationToken = default)
    {
        var query = _context.Lots
            .Include(l => l.Bids)
            .Include(l => l.Images)
            .Where(l => l.SellerId == sellerId && !l.IsDeleted);

        if (!includeDrafts)
        {
            query = query.Where(l => l.Status != LotStatus.Draft && l.Status != LotStatus.Rejected);
        }

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Lot>> GetByWinnerIdAsync(Guid winnerId, CancellationToken cancellationToken = default)
    {
        return await _context.Lots
            .Include(l => l.Bids)
            .Include(l => l.Images)
            .Where(l => l.WinnerId == winnerId && !l.IsDeleted)
            .OrderByDescending(l => l.EndTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        await _context.Lots.AddAsync(lot, cancellationToken);
    }

    public Task UpdateAsync(Lot lot, CancellationToken cancellationToken = default)
    {
        // Not needed - EF Core automatically tracks changes for entities loaded via GetByIdAsync
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
