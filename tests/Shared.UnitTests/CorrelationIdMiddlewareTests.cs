using AuHub.Shared.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace Shared.UnitTests;

public class CorrelationIdMiddlewareTests
{
    private static async Task InvokeMiddlewareAsync(HttpContext context, RequestDelegate? next = null)
    {
        next ??= _ => Task.CompletedTask;
        var middleware = new CorrelationIdMiddleware(next);
        await middleware.InvokeAsync(context);
    }

    [Fact]
    public async Task WhenHeaderExists_UsesExistingCorrelationId()
    {
        var context = new DefaultHttpContext();
        var expectedId = "existing-correlation-id";
        context.Request.Headers["X-Correlation-Id"] = expectedId;

        await InvokeMiddlewareAsync(context);

        context.TraceIdentifier.Should().Be(expectedId);
    }

    [Fact]
    public async Task WhenHeaderMissing_GeneratesNewCorrelationId()
    {
        var context = new DefaultHttpContext();

        await InvokeMiddlewareAsync(context);

        context.TraceIdentifier.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GeneratedId_IsGuid()
    {
        var context = new DefaultHttpContext();

        await InvokeMiddlewareAsync(context);

        Guid.TryParse(context.TraceIdentifier, out _).Should().BeTrue();
    }

    [Fact]
    public async Task SetsRequestHeader_Downstream()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "req-id";

        await InvokeMiddlewareAsync(context);

        ((string)context.Request.Headers["X-Correlation-Id"]!).Should().Be("req-id");
    }

    [Fact]
    public async Task GeneratedCorrelationId_PropagatesToRequestHeader()
    {
        var context = new DefaultHttpContext();

        await InvokeMiddlewareAsync(context);

        ((string)context.Request.Headers["X-Correlation-Id"]!).Should().Be(context.TraceIdentifier);
    }

    [Fact]
    public async Task CallsNextDelegate()
    {
        var context = new DefaultHttpContext();
        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };

        var middleware = new CorrelationIdMiddleware(next);
        await middleware.InvokeAsync(context);

        called.Should().BeTrue();
    }
}
