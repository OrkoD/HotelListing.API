namespace HotelListing.Api.Application.DTOs.Hotel;

public record HotelImportRowDto
{
    /// <summary>Line/record index in the file (e.g. Row 14), used for granular error reporting.</summary>
    public int RowNumber { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;

    public double Rating { get; init; }

    public decimal PerNightRate { get; init; }

    /// <summary>Country name or short code (e.g., "Jamaica" or "JM") to resolve CountryId.</summary>
    public string CountryIdentifier { get; init; } = string.Empty;
}

