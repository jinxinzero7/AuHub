using Identity.Domain.Interfaces;
using System.Security.Claims;

namespace Identity.API.Middleware;

public class BanMiddleware
{
    private readonly RequestDelegate _next;

    public BanMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IUserRepository userRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

            // Skip auth endpoints (login, register, refresh)
            if (!path.StartsWith("/api/auth/"))
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
                {
                    var user = await userRepository.GetByIdAsync(userId);
                    if (user != null && user.IsBanned)
                    {
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsJsonAsync(new { error = "Your account has been banned", reason = user.BanReason });
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}
