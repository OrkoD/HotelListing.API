using Microsoft.EntityFrameworkCore;
using HotelListing.Api.Common.Models.Paging;

namespace HotelListing.Api.Common.Models.Extensions;

public static class QueryableExtensions
{
    public static async Task<PageResult<T>> ToPageResultAsync<T>(
        this IQueryable<T> source,
        PaginationParameters paginationParameters
    )
    {
        var totalCount = await source.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)paginationParameters.PageSize);

        var items = await source
            .Skip((paginationParameters.PageNumber - 1) * paginationParameters.PageSize)
            .Take(paginationParameters.PageSize)
            .ToListAsync();

        var metadata = new PaginationMetadata
        {
            CurrentPage = paginationParameters.PageNumber,
            PageSize = paginationParameters.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNext = paginationParameters.PageNumber < totalPages,
            HasPrevious = paginationParameters.PageNumber > 1
        };

        return new PageResult<T>
        {
            Data = items,
            Metadata = metadata
        };
    }
}
