using System.Net.Http.Json;
using System.Text.Json;
using Auctions.Application.Services;
using Microsoft.Extensions.Logging;

namespace Auctions.Infrastructure.Services;

public class PaymentClient : IPaymentClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentClient> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PaymentClient(HttpClient httpClient, ILogger<PaymentClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BalanceResult> GetBalanceAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/payment/balance?userId={userId}", ct);
            if (response.IsSuccessStatusCode)
            {
                var dto = await response.Content.ReadFromJsonAsync<BalanceResponseDto>(JsonOptions, ct);
                if (dto != null)
                    return new BalanceResult(true, dto.Balance);
                return new BalanceResult(false, 0, "Empty response");
            }
            _logger.LogWarning("Failed to get balance for user {UserId}. Status: {StatusCode}", userId, response.StatusCode);
            return new BalanceResult(false, 0, $"HTTP {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for user {UserId}", userId);
            return new BalanceResult(false, 0, ex.Message);
        }
    }

    public async Task<PaymentResult> ReserveFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default)
    {
        return await ExecutePaymentAsync("/api/payment/reserve", new { UserId = userId, Amount = amount, LotId = lotId }, ct);
    }

    public async Task<PaymentResult> ReleaseFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default)
    {
        return await ExecutePaymentAsync("/api/payment/release", new { UserId = userId, Amount = amount, LotId = lotId }, ct);
    }

    public async Task<PaymentResult> ChargeWinnerAsync(Guid winnerId, decimal amount, Guid lotId, CancellationToken ct = default)
    {
        return await ExecutePaymentAsync("/api/payment/charge-winner", new { UserId = winnerId, Amount = amount, LotId = lotId }, ct);
    }

    public async Task<PaymentResult> TransferToSellerAsync(Guid sellerId, decimal amount, Guid lotId, CancellationToken ct = default)
    {
        return await ExecutePaymentAsync("/api/payment/transfer-seller", new { UserId = sellerId, Amount = amount, LotId = lotId }, ct);
    }

    public async Task<PaymentResult> RefundFundsAsync(Guid userId, decimal amount, Guid lotId, CancellationToken ct = default)
    {
        return await ExecutePaymentAsync("/api/payment/refund", new { UserId = userId, Amount = amount, LotId = lotId }, ct);
    }

    private async Task<PaymentResult> ExecutePaymentAsync(string endpoint, object payload, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, payload, ct);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PaymentResult>(JsonOptions, ct);
                return result ?? new PaymentResult(false, "Empty response");
            }
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Payment failed at {Endpoint}. Status: {StatusCode}, Response: {Response}",
                endpoint, response.StatusCode, errorContent);
            return new PaymentResult(false, $"HTTP {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing payment at {Endpoint}", endpoint);
            return new PaymentResult(false, ex.Message);
        }
    }

    private record BalanceResponseDto(Guid UserId, decimal Balance, decimal FrozenBalance);
}
