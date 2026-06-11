using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;
using Auctions.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Auctions.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly AuctionsDbContext _context;

    public ReviewRepository(AuctionsDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByLotIdAsync(Guid lotId, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(review => review.LotId == lotId, cancellationToken);
    }

    public async Task<List<Review>> GetBySellerIdAsync(Guid sellerId, CancellationToken cancellationToken = default)
    {
        return await _context.Reviews
            .Where(review => review.SellerId == sellerId)
            .OrderByDescending(review => review.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Review review, CancellationToken cancellationToken = default)
    {
        await _context.Reviews.AddAsync(review, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
