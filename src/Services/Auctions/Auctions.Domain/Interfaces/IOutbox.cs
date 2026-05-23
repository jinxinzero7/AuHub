namespace Auctions.Domain.Interfaces;

public interface IOutbox
{
    Task AddAsync(string type, string payload, CancellationToken cancellationToken = default);
}
