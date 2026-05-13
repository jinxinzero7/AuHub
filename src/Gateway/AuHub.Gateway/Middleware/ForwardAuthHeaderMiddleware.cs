namespace AuHub.Gateway.Middleware;

public class ForwardAuthHeaderMiddleware
{
    private readonly RequestDelegate _next;

    public ForwardAuthHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(authHeader))
        {
            context.Items["X-Forwarded-Authorization"] = authHeader;
        }

        await _next(context);
    }
}