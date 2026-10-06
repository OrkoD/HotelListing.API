using System.Security.Claims;
using Serilog;
using Serilog.Events;

namespace Microsoft.AspNetCore.Builder;

public static class LoggingExtensions
{
    public static IApplicationBuilder UseCustomSerilogRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

            options.GetLevel = (httpContext, elapsed, ex) => ex != null
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode >= 500
                    ? LogEventLevel.Error
                    : httpContext.Response.StatusCode >= 400
                        ? LogEventLevel.Warning
                        : LogEventLevel.Information;

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                var username = httpContext.User?.Identity?.Name
                            ?? httpContext.User?.FindFirst(ClaimTypes.Email)?.Value
                            ?? "anonymous";

                diagnosticContext.Set("Username", username);
                diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                if (httpContext.User?.Identity?.IsAuthenticated == true)
                {
                    var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? httpContext.User.FindFirst("sub")?.Value
                        ?? "unknown";
                    diagnosticContext.Set("UserId", userId);
                }
            };
        });
    }
}
