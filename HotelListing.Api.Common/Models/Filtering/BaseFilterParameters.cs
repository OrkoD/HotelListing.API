namespace HotelListing.Api.Common.Models.Filtering;

public abstract class BaseFilterParameters
{
    public string? Search { get; set; } = string.Empty;
    public string? SortBy { get; set; } = string.Empty;
    public bool SortDescending { get; set; } = false;
}
