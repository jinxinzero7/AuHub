using Auctions.Application.Commands.CompleteLot;
using FastEndpoints;

namespace Auctions.API.Endpoints.Lots;

public class CompleteLotEndpoint : EndpointWithoutRequest<CompleteLotResponse>
{
    private readonly CompleteLotCommandHandler _handler;

    public CompleteLotEndpoint(CompleteLotCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/admin/lots/{id}/force-complete");
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Force-complete a lot (Admin only)";
            s.Description = "Administrative fallback to complete an active auction lot manually.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lotId = Route<Guid>("id");

        var command = new CompleteLotCommand
        {
            LotId = lotId
        };

        var result = await _handler.HandleAsync(command, ct);

        if (!result.IsSuccess)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        Response = result.Value;
    }
}
