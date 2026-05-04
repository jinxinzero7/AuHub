using Microsoft.EntityFrameworkCore;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;
using Auctions.Infrastructure.Data;

namespace Auctions.Infrastructure.Repositories;

public class BidRepository : IBidRepository
{
    private readonly AuctionsDbContext _context;

    public BidRepository(AuctionsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Bid bid, CancellationToken cancellationToken = default)
    {
        await _context.Bids.AddAsync(bid, cancellationToken);
    }

    public async Task<List<Bid>> GetByLotIdAsync(Guid lotId, CancellationToken cancellationToken = default)
    {
        return await _context.Bids
            .Where(b => b.LotId == lotId)
            .OrderByDescending(b => b.PlacedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
