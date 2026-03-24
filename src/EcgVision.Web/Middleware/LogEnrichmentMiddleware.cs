using System.Security.Claims;

using Serilog.Context;

namespace EcgVision.Web.Middleware;

public class LogEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Extract the data safely
        var userIdentifier = context.User?.FindFirstValue(ClaimTypes.Name)
                            ?? context.User?.FindFirstValue(ClaimTypes.Email)
                            ?? "Anonymous";
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        // 2. Push to Serilog Context (The magic happens here)
        using (LogContext.PushProperty("UserId", userIdentifier))
        using (LogContext.PushProperty("IPAddress", ip))
        using (LogContext.PushProperty("UserAgent", userAgent))
        {
            // 3. Continue the request
            await next(context);
        }
    }
}
