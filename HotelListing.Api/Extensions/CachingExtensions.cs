using HotelListing.Api.CachePolicies;
using HotelListing.Api.Common.Constants;

namespace Microsoft.Extensions.DependencyInjection;

public static class CachingExtensions
{
    public static IServiceCollection AddCachingServices(this IServiceCollection services)
    {
        services.AddOutputCache(options =>
        {
            options.AddPolicy(CacheConstants.AuthenticatedUserCachingPolicy, policyBuilder =>
            {
                policyBuilder.AddPolicy<AuthenticatedUserCachingPolicy>()
                             .SetCacheKeyPrefix(CacheConstants.AuthenticatedUserCachingPolicyTag);
            }, excludeDefaultPolicy: true);
        });

        return services;
    }
}
