using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuHub.Shared.Security;

public static class InternalApiKey
{
    public const string HeaderName = "X-Internal-Api-Key";

    public static bool IsValid(HttpContext context)
    {
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var expected = TryGetExpectedValue(configuration);
        var actual = context.Request.Headers[HeaderName].FirstOrDefault();

        return expected is not null && IsMatch(actual, expected);
    }

    public static string GetExpectedValue(IConfiguration configuration)
    {
        return TryGetExpectedValue(configuration)
            ?? throw new InvalidOperationException("Internal API key is not configured");
    }

    private static string? TryGetExpectedValue(IConfiguration configuration)
    {
        var value = configuration["InternalApiKey"]
            ?? configuration["InternalApiKey:Value"]
            ?? Environment.GetEnvironmentVariable("INTERNAL_API_KEY");

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool IsMatch(string? actual, string expected)
    {
        if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(expected))
            return false;

        var actualBytes = Encoding.UTF8.GetBytes(actual);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);

        return actualBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
