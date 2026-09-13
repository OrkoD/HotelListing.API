namespace HotelListing.Api.Common.Models.Paging;

public class CursorPaginationMetadata
{
    public string? PreviousCursor { get; set; }
    public string? NextCursor { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public int PageSize { get; set; }
}
