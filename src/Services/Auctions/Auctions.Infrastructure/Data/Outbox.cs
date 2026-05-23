using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;
using Auctions.Infrastructure.Data;

namespace Auctions.Infrastructure.Data;

public class Outbox : IOutbox
{
    private readonly AuctionsDbContext _context;

    public Outbox(AuctionsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(string type, string payload, CancellationToken cancellationToken = default)
    {
        var message = OutboxMessage.Create(type, payload);
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }
}
