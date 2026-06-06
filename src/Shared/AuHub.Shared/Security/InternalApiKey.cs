using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuHub.Shared.Security;

public static class InternalApiKey
{
    public const string HeaderName = "X-Internal-Api-Key";
    private const string DefaultValue = "AuHub-Internal-Secret-2026";

    public static bool IsValid(HttpContext context)
    {
        var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
        var expected = GetExpectedValue(configuration);
        var actual = context.Request.Headers[HeaderName].FirstOrDefault();

        return IsMatch(actual, expected);
    }

    public static string GetExpectedValue(IConfiguration configuration)
    {
        return configuration["InternalApiKey"]
            ?? configuration["InternalApiKey:Value"]
            ?? Environment.GetEnvironmentVariable("INTERNAL_API_KEY")
            ?? DefaultValue;
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
