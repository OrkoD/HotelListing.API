namespace HotelListing.Api.Common.Models.Paging;

public class CursorPageResult<T>
{
    public IEnumerable<T> Data { get; set; } = [];
    public CursorPaginationMetadata Metadata { get; set; } = new();
}
