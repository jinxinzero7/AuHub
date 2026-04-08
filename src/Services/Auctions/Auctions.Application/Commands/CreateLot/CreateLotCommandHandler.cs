using Auctions.Domain.Common;
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
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lot = Lot.Create(
                command.Title,
                command.Description,
                command.StartingPrice,
                command.StartTime,
                command.EndTime,
                command.SellerId);

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
