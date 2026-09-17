using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HotelListing.Api.Middleware;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // 1. Correlate with current activity or fallback to HTTP trace identifier
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        // 2. Structured log with full exception stack trace for diagnostics in Seq/Files
        logger.LogError(
            exception,
            "An unhandled exception occurred while processing the request. TraceId: {TraceId}, Path: {Path}, Method: {Method}",
            traceId,
            httpContext.Request.Path,
            httpContext.Request.Method
        );

        var isDevelopment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();

        // 3. Construct RFC 7807 standardized ProblemDetails response
        var problemDetails = new ProblemDetails
        {
            Title = "An error occurred while processing your request.",
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.1",
            Instance = httpContext.Request.Path,
            // Prevent sensitive stack traces or internal exception details from leaking in Production
            Detail = isDevelopment
                ? exception.Message
                : "An unexpected error occurred. Please try again later."
        };

        // Attach traceId so clients can quote it for support tickets
        problemDetails.Extensions["traceId"] = traceId;

        if (isDevelopment)
        {
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
        }

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // Return true to indicate the exception has been fully handled and response written
        return true;
    }
}
