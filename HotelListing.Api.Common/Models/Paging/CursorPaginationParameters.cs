using System.ComponentModel.DataAnnotations;

namespace HotelListing.Api.Common.Models.Paging;

public class CursorPaginationParameters
{
    private const int MaxPageSize = 50;

    private int _pageSize = 10;

    /// <summary>
    /// Opaque Base64 token representing the anchor point of the last item from the previous page.
    /// Null on the first page.
    /// </summary>
    [MaxLength(200, ErrorMessage = "Cursor token is too long.")]
    public string? Cursor { get; set; }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 1 : (value > MaxPageSize ? MaxPageSize : value);
    }
}
