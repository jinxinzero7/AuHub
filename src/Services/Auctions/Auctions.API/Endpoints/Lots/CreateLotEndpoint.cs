using Auctions.Application.Commands.CreateLot;
using FastEndpoints;
using System.Security.Claims;

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
        Roles("Admin");
        Summary(s =>
        {
            s.Summary = "Create a new auction lot (Admin only)";
            s.Description = "Creates a new lot for auction with starting price and time range. Requires Admin role.";
        });
    }

    public override async Task HandleAsync(CreateLotRequest req, CancellationToken ct)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrEmpty(req.Title) || req.Title.Length < 3)
            errors.Add("Title must be at least 3 characters");
        if (req.Title.Length > 200)
            errors.Add("Title must not exceed 200 characters");
        if (string.IsNullOrEmpty(req.Description))
            errors.Add("Description is required");
        if (req.Description.Length > 2000)
            errors.Add("Description must not exceed 2000 characters");
        if (req.StartingPrice <= 0)
            errors.Add("Starting price must be greater than 0");
        if (req.StartTime <= DateTime.UtcNow)
            errors.Add("Start time must be in the future");
        if (req.EndTime <= req.StartTime)
            errors.Add("End time must be after start time");

        if (errors.Any())
        {
            ThrowError(string.Join("; ", errors), 400);
            return;
        }

        var sellerIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sellerIdClaim) || !Guid.TryParse(sellerIdClaim, out var sellerId))
        {
            ThrowError("Invalid user ID in token", 401);
            return;
        }
        
        var command = new CreateLotCommand
        {
            Title = req.Title,
            Description = req.Description,
            StartingPrice = req.StartingPrice,
            StartTime = req.StartTime,
            EndTime = req.EndTime
        };

        var result = await _handler.HandleAsync(command, sellerId, ct);

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

public record CreateLotRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal StartingPrice { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
}

public record CreateLotResponse
{
    public bool Success { get; init; }
    public Guid? LotId { get; init; }
    public string? Error { get; init; }
}
