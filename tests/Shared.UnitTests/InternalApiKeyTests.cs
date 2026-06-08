using AuHub.Shared.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.UnitTests;

public class InternalApiKeyTests
{
    [Fact]
    public void GetExpectedValue_WhenMissing_Throws()
    {
        var configuration = new ConfigurationBuilder().Build();

        var act = () => InternalApiKey.GetExpectedValue(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Internal API key is not configured");
    }

    [Fact]
    public void GetExpectedValue_WhenBlank_Throws()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalApiKey"] = ""
            })
            .Build();

        var act = () => InternalApiKey.GetExpectedValue(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Internal API key is not configured");
    }

    [Fact]
    public void GetExpectedValue_WhenConfigured_ReturnsValue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalApiKey"] = "configured-key"
            })
            .Build();

        InternalApiKey.GetExpectedValue(configuration).Should().Be("configured-key");
    }

    [Fact]
    public void IsValid_WhenExpectedKeyMissing_ReturnsFalse()
    {
        var context = CreateContext(new ConfigurationBuilder().Build());
        context.Request.Headers[InternalApiKey.HeaderName] = "any-key";

        InternalApiKey.IsValid(context).Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenKeyMatches_ReturnsTrue()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalApiKey"] = "configured-key"
            })
            .Build();
        var context = CreateContext(configuration);
        context.Request.Headers[InternalApiKey.HeaderName] = "configured-key";

        InternalApiKey.IsValid(context).Should().BeTrue();
    }

    private static DefaultHttpContext CreateContext(IConfiguration configuration)
    {
        var services = new ServiceCollection()
            .AddSingleton(configuration)
            .BuildServiceProvider();

        return new DefaultHttpContext { RequestServices = services };
    }
}
