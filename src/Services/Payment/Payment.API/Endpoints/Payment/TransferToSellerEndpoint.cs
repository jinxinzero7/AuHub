using AuHub.Shared.ValueObjects;
using FastEndpoints;
using Payment.Application.Commands.TransferToSeller;

namespace Payment.API.Endpoints.Payment;

public class TransferToSellerEndpoint : Endpoint<TransferToSellerRequest, PaymentOperationResponse>
{
    private readonly TransferToSellerCommandHandler _handler;

    public TransferToSellerEndpoint(TransferToSellerCommandHandler handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/payment/transfer-seller");
        AllowAnonymous(); // Internal service-to-service call
        Summary(s =>
        {
            s.Summary = "Transfer funds to seller";
            s.Description = "Deposit funds to seller's wallet after commission (internal).";
        });
    }

    public override async Task HandleAsync(TransferToSellerRequest req, CancellationToken ct)
    {
        if (req.Amount.Amount <= 0)
        {
            ThrowError("Amount must be greater than 0", 400);
            return;
        }

        var command = new TransferToSellerCommand
        {
            SellerId = req.UserId,
            Amount = req.Amount,
            ReferenceId = req.LotId
        };

        var result = await _handler.HandleAsync(command, ct);

        if (result.IsFailure)
        {
            ThrowError(result.Error, result.StatusCode);
            return;
        }

        Response = new PaymentOperationResponse(true, "Funds transferred to seller");
    }
}

public record TransferToSellerRequest
{
    public Guid UserId { get; init; }
    public Money Amount { get; init; } = Money.Zero;
    public Guid LotId { get; init; }
}
