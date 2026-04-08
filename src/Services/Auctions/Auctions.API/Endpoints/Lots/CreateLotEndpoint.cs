using Auctions.Application.Commands.CreateLot;
using FastEndpoints;

namespace Auctions.API.Endpoints.Lots;

public class CreateLotEndpoint : Endpoint<CreateLotRequest, CreateLotResponse>
{
    private readonly CreateLotCommandHandler _handler;

    public CreateLotEndpoint(CreateLotCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/lots");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new auction lot";
            s.Description = "Creates a new lot for auction with starting price and time range";
        });
    }

    public override async Task HandleAsync(CreateLotRequest req, CancellationToken ct)
    {
        var command = new CreateLotCommand
        {
            Title = req.Title,
            Description = req.Description,
            StartingPrice = req.StartingPrice,
            StartTime = req.StartTime,
            EndTime = req.EndTime,
            SellerId = req.SellerId
        };

        var result = await _handler.HandleAsync(command, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
        }

        Response = new CreateLotResponse
        {
            Success = true,
            LotId = result.Value
        };
        
        HttpContext.Response.StatusCode = 201;
    }
}

// Placeholder endpoint for CreatedAt
public class GetLotByIdEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/api/lots/{id}");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) => Task.CompletedTask;
}

public record CreateLotRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal StartingPrice { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public Guid SellerId { get; init; }
}

public record CreateLotResponse
{
    public bool Success { get; init; }
    public Guid? LotId { get; init; }
    public string? Error { get; init; }
}
