using Auctions.Application.Commands.CreateLot;
using Auctions.Domain.Enums;
using AuHub.Shared.ValueObjects;
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
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Create a new auction lot";
            s.Description = "Creates a new lot for auction with starting price and duration. Requires authentication.";
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
        if (req.StartingPrice.Amount <= 0)
            errors.Add("Starting price must be greater than 0");
        if (req.DurationHours <= 0)
            errors.Add("Duration must be greater than 0");
        if (req.DurationHours > 720)
            errors.Add("Duration must not exceed 720 hours (30 days)");

        var deliveryProviders = new List<DeliveryProvider>();
        if (req.SupportedDeliveryProviders.Count == 0)
        {
            errors.Add("At least one delivery provider is required");
        }
        else
        {
            foreach (var provider in req.SupportedDeliveryProviders)
            {
                if (!Enum.TryParse<DeliveryProvider>(provider, ignoreCase: true, out var parsedProvider))
                {
                    errors.Add($"Unsupported delivery provider: {provider}");
                    continue;
                }

                deliveryProviders.Add(parsedProvider);
            }
        }

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
            DurationHours = req.DurationHours,
            SupportedDeliveryProviders = deliveryProviders
        };

        var result = await _handler.HandleAsync(command, sellerId, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
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
    public Money StartingPrice { get; init; } = Money.Zero;
    public int DurationHours { get; init; }
    public List<string> SupportedDeliveryProviders { get; init; } = new();
}

public record CreateLotResponse
{
    public bool Success { get; init; }
    public Guid? LotId { get; init; }
    public string? Error { get; init; }
}
