using System.Security.Claims;
using System.Threading.RateLimiting;
using HotelListing.Api.Common.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace Microsoft.Extensions.DependencyInjection;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddRateLimitingServices(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            // A. Fixed Window Policy (for simple endpoints)
            options.AddFixedWindowLimiter(RateLimitingConstants.FixedPolicy, opt =>
            {
                opt.Window = TimeSpan.FromMinutes(1);
                opt.PermitLimit = 50;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            // B. Sliding Window Policy (per authenticated user)
            options.AddPolicy(RateLimitingConstants.PerUserPolicy, context =>
            {
                var username = context.User?.FindFirst(ClaimTypes.Email)?.Value
                            ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? context.User?.Identity?.Name
                            ?? RateLimitingConstants.AnonymousUser;

                return RateLimitPartition.GetSlidingWindowLimiter(username, _ => new SlidingWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 50,
                    SegmentsPerWindow = 6,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 3
                });
            });

            // C. Global Limiter (per client IP address)
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? RateLimitingConstants.UnknownIp;

                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 200,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = retryAfter.TotalSeconds
                }, cancellationToken: cancellationToken);
            };
        });

        return services;
    }
}
