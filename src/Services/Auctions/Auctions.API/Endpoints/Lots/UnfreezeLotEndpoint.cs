using FastEndpoints;
using Auctions.Domain.Interfaces;

namespace Auctions.API.Endpoints.Lots;

public class UnfreezeLotEndpoint : EndpointWithoutRequest
{
    private readonly ILotRepository _lotRepository;

    public UnfreezeLotEndpoint(ILotRepository lotRepository)
    {
        _lotRepository = lotRepository;
    }

    public override void Configure()
    {
        Post("/api/lots/{id}/unfreeze");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Unfreeze a lot (Admin only)";
            s.Description = "Unfreeze a frozen lot and return it to Active status.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        var lot = await _lotRepository.GetByIdAsync(lotId, ct);
        if (lot == null)
        {
            ThrowError("Lot not found", 404);
            return;
        }

        lot.Unfreeze();
        await _lotRepository.SaveChangesAsync(ct);

        Response = new { Success = true, Message = "Lot unfrozen" };
    }
}
