using Microsoft.EntityFrameworkCore;
using HotelListing.Api.Common.Models.Paging;

using HotelListing.Api.Common.Contracts;

namespace HotelListing.Api.Common.Models.Extensions;

public static class QueryableExtensions
{
    public static async Task<PageResult<T>> ToPageResultAsync<T>(
        this IQueryable<T> source,
        PaginationParameters paginationParameters,
        CancellationToken cancellationToken = default
    )
    {
        var totalCount = await source.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)paginationParameters.PageSize);

        var items = await source
            .Skip((paginationParameters.PageNumber - 1) * paginationParameters.PageSize)
            .Take(paginationParameters.PageSize)
            .ToListAsync(cancellationToken);

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

    public static Task<CursorPageResult<T>> ToCursorResultAsync<T>(
        this IQueryable<T> source,
        CursorPaginationParameters parameters,
        CancellationToken cancellationToken = default
    ) where T : class, IIdentifiable =>
        source.ToCursorResultAsync(parameters, q => q, cancellationToken);

    public static async Task<CursorPageResult<TResult>> ToCursorResultAsync<TSource, TResult>(
        this IQueryable<TSource> source,
        CursorPaginationParameters parameters,
        Func<IQueryable<TSource>, IQueryable<TResult>> project,
        CancellationToken cancellationToken = default
    )
        where TSource : class, IIdentifiable
        where TResult : class, IIdentifiable
    {
        var token = CursorToken.Decode(parameters.Cursor);
        var pageSize = parameters.PageSize;
        var isBackward = token?.IsBackward == true;

        var query = source;

        if (token != null)
        {
            query = isBackward
                ? query.Where(x => x.Id < token.Id).OrderByDescending(x => x.Id)
                : query.Where(x => x.Id > token.Id).OrderBy(x => x.Id);
        }
        else
        {
            query = query.OrderBy(x => x.Id);
        }

        var items = await project(query.Take(pageSize + 1)).ToListAsync(cancellationToken);
        var hasMore = items.Count > pageSize;

        if (hasMore)
            items.RemoveAt(items.Count - 1);

        if (isBackward)
            items.Reverse();

        var hasNext = isBackward || hasMore;
        var hasPrevious = (!isBackward && token != null) || (isBackward && hasMore);

        return new CursorPageResult<TResult>
        {
            Data = items,
            Metadata = new CursorPaginationMetadata
            {
                NextCursor = hasNext && items.Count > 0 ? CursorToken.Encode(items[^1].Id, false) : null,
                PreviousCursor = hasPrevious && items.Count > 0 ? CursorToken.Encode(items[0].Id, true) : null,
                HasNext = hasNext,
                HasPrevious = hasPrevious,
                PageSize = pageSize
            }
        };
    }
}
