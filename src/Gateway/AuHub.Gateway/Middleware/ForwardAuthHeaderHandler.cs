namespace AuHub.Gateway.Middleware;

public class ForwardAuthHeaderHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Forward Authorization header to downstream services
        var authHeader = request.Headers.Authorization;
        if (authHeader != null)
        {
            if (!request.Headers.Contains("Authorization"))
            {
                request.Headers.Add("Authorization", authHeader.ToString());
            }
        }
        return base.SendAsync(request, cancellationToken);
    }
}