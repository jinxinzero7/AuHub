using System.Security.Claims;
using Auctions.Application.Commands.UpdateLot;
using Auctions.Domain.Enums;
using AuHub.Shared.ValueObjects;
using FastEndpoints;

namespace Auctions.API.Endpoints.Lots;

public class UpdateLotEndpoint : Endpoint<UpdateLotRequest, UpdateLotResponse>
{
    private readonly UpdateLotCommandHandler _handler;

    public UpdateLotEndpoint(UpdateLotCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Put("/api/lots/{id}");
        Roles("User");
        Summary(s =>
        {
            s.Summary = "Update draft or rejected lot";
            s.Description = "Allows the seller to edit a Draft or Rejected lot. PendingModeration and active/finalized lots cannot be edited.";
        });
    }

    public override async Task HandleAsync(UpdateLotRequest req, CancellationToken ct)
    {
        var errors = ValidateRequest(req, out var deliveryProviders);
        if (errors.Count > 0)
        {
            ThrowError(string.Join("; ", errors), 400);
            return;
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID in token", 401);
            return;
        }

        var command = new UpdateLotCommand
        {
            LotId = Route<Guid>("id"),
            Title = req.Title,
            Description = req.Description,
            StartingPrice = req.StartingPrice,
            DurationHours = req.DurationHours,
            SupportedDeliveryProviders = deliveryProviders,
            SubmitForModeration = req.SubmitForModeration
        };

        var result = await _handler.HandleAsync(command, userId, ct);
        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        Response = result.Value;
    }

    private static List<string> ValidateRequest(UpdateLotRequest req, out List<DeliveryProvider> deliveryProviders)
    {
        var errors = new List<string>();
        deliveryProviders = new List<DeliveryProvider>();

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

        return errors;
    }
}

public record UpdateLotRequest
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Money StartingPrice { get; init; } = Money.Zero;
    public int DurationHours { get; init; }
    public List<string> SupportedDeliveryProviders { get; init; } = new();
    public bool SubmitForModeration { get; init; }
}
