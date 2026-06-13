using FastEndpoints;
using Payment.Application.Commands.ConfirmTopUp;
using Payment.Application.Services;

namespace Payment.API.Endpoints.Payment;

public class RobokassaTopUpResultEndpoint : EndpointWithoutRequest
{
    private readonly IPaymentCheckoutProvider _paymentCheckoutProvider;
    private readonly ConfirmTopUpCommandHandler _confirmTopUpCommandHandler;

    public RobokassaTopUpResultEndpoint(
        IPaymentCheckoutProvider paymentCheckoutProvider,
        ConfirmTopUpCommandHandler confirmTopUpCommandHandler)
    {
        _paymentCheckoutProvider = paymentCheckoutProvider;
        _confirmTopUpCommandHandler = confirmTopUpCommandHandler;
    }

    public override void Configure()
    {
        Post("/api/payment/topup/robokassa/result");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var parameters = await ReadCallbackParametersAsync(ct);
        var confirmationResult = _paymentCheckoutProvider.ConfirmTopUpCallback(parameters);
        if (confirmationResult.IsFailure)
        {
            ThrowError(confirmationResult.Error, confirmationResult.StatusCode);
            return;
        }

        var confirmation = confirmationResult.Value;
        var commandResult = await _confirmTopUpCommandHandler.HandleAsync(new ConfirmTopUpCommand
        {
            UserId = confirmation.UserId,
            Amount = confirmation.Amount,
            OperationId = confirmation.OperationId,
            Provider = confirmation.Provider
        }, ct);

        if (commandResult.IsFailure)
        {
            ThrowError(commandResult.Error, commandResult.StatusCode);
            return;
        }

        HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await HttpContext.Response.WriteAsync($"OK{confirmation.InvoiceId}", ct);
    }

    private async Task<Dictionary<string, string>> ReadCallbackParametersAsync(CancellationToken ct)
    {
        if (HttpContext.Request.HasFormContentType)
        {
            var form = await HttpContext.Request.ReadFormAsync(ct);
            return form.ToDictionary(field => field.Key, field => field.Value.ToString());
        }

        return HttpContext.Request.Query.ToDictionary(field => field.Key, field => field.Value.ToString());
    }
}
