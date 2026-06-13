using System.Security.Claims;
using AuHub.Shared.ValueObjects;
using FastEndpoints;
using Payment.Application.Services;

namespace Payment.API.Endpoints.Payment;

public class CreateTopUpCheckoutEndpoint : Endpoint<CreateTopUpCheckoutRequest, CreateTopUpCheckoutResponse>
{
    private readonly IPaymentCheckoutProvider _paymentCheckoutProvider;

    public CreateTopUpCheckoutEndpoint(IPaymentCheckoutProvider paymentCheckoutProvider)
    {
        _paymentCheckoutProvider = paymentCheckoutProvider;
    }

    public override void Configure()
    {
        Post("/api/payment/topup/checkout");
        Roles("Admin", "User");
    }

    public override async Task HandleAsync(CreateTopUpCheckoutRequest req, CancellationToken ct)
    {
        if (req.Amount.Amount <= 0)
        {
            ThrowError("Amount must be greater than 0", 400);
            return;
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Invalid user ID", 401);
            return;
        }

        var operationId = Guid.NewGuid();
        var invoiceId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var checkoutResult = _paymentCheckoutProvider.CreateTopUpCheckout(
            userId,
            req.Amount,
            operationId,
            invoiceId);

        if (checkoutResult.IsFailure)
        {
            ThrowError(checkoutResult.Error, checkoutResult.StatusCode);
            return;
        }

        var checkout = checkoutResult.Value;
        Response = new CreateTopUpCheckoutResponse
        {
            Success = true,
            Provider = checkout.Provider,
            OperationId = checkout.OperationId,
            InvoiceId = checkout.InvoiceId,
            PaymentUrl = checkout.PaymentUrl,
            IsTest = checkout.IsTest
        };

        await Task.CompletedTask;
    }
}

public record CreateTopUpCheckoutRequest
{
    public Money Amount { get; init; } = Money.Zero;
}

public record CreateTopUpCheckoutResponse
{
    public bool Success { get; init; }
    public string Provider { get; init; } = string.Empty;
    public Guid OperationId { get; init; }
    public long InvoiceId { get; init; }
    public string PaymentUrl { get; init; } = string.Empty;
    public bool IsTest { get; init; }
}
