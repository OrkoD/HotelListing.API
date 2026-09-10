using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Application.Services;

public class ApiKeyValidatorService(HotelListingDbContext db) : IApiKeyValidatorService
{
    public async Task<bool> IsValidAsync(string apiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        var apiKeyEntity = await db.ApiKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(k => k.Key == apiKey, ct);

        if (apiKeyEntity is null) return false;

        return apiKeyEntity.IsActive;
    }
}
