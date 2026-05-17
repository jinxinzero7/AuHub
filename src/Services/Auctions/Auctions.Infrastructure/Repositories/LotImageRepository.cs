using Microsoft.EntityFrameworkCore;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;
using Auctions.Infrastructure.Data;

namespace Auctions.Infrastructure.Repositories;

public class LotImageRepository : ILotImageRepository
{
    private readonly AuctionsDbContext _context;

    public LotImageRepository(AuctionsDbContext context)
    {
        _context = context;
    }

    public async Task<LotImage?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.LotImages.FindAsync(new object[] { id }, ct);
    }

    public async Task<IReadOnlyList<LotImage>> GetByLotIdAsync(Guid lotId, CancellationToken ct = default)
    {
        return await _context.LotImages
            .Where(i => i.LotId == lotId)
            .OrderByDescending(i => i.UploadedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(LotImage image, CancellationToken ct = default)
    {
        await _context.LotImages.AddAsync(image, ct);
    }

    public void Remove(LotImage image)
    {
        _context.LotImages.Remove(image);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
