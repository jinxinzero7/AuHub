using AuHub.Shared.Results;
using Auctions.Domain.Entities;
using Auctions.Domain.Interfaces;

namespace Auctions.Application.Commands.CreateLot;

public class CreateLotCommandHandler
{
    private readonly ILotRepository _lotRepository;

    public CreateLotCommandHandler(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public async Task<Result<Guid>> HandleAsync(
        CreateLotCommand command,
        Guid sellerId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var duration = TimeSpan.FromHours(command.DurationHours);

            var lot = Lot.Create(
                command.Title,
                command.Description,
                command.StartingPrice,
                duration,
                sellerId);

            await _lotRepository.AddAsync(lot, cancellationToken);
            await _lotRepository.SaveChangesAsync(cancellationToken);

            return Result.Success(lot.Id);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>($"Failed to create lot: {ex.Message}", 500);
        }
    }
}
