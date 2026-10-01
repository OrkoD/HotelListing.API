using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Results;

namespace HotelListing.Api.Application.Services.Parsers;

/// <summary>
/// Streams and parses JSON hotel feeds item-by-item without buffering the full document in memory.
/// </summary>
public sealed class JsonHotelParser : IHotelDataParser
{
    public SupportedImportFormat Format => SupportedImportFormat.Json;

    public string SupportedExtension => ".json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public async IAsyncEnumerable<Result<HotelImportRowDto>> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var rowNumber = 0;

        await foreach (var item in JsonSerializer.DeserializeAsyncEnumerable<HotelImportJsonModel>(
            stream,
            JsonOptions,
            cancellationToken))
        {
            rowNumber++;

            if (item is null)
            {
                yield return Failure(rowNumber, "Empty or null JSON object encountered.");
                continue;
            }

            yield return ParseRow(item, rowNumber);
        }
    }

    private static Result<HotelImportRowDto> ParseRow(
        HotelImportJsonModel item,
        int rowNumber)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
            return Failure(rowNumber, "Hotel Name is required.");

        if (string.IsNullOrWhiteSpace(item.Address))
            return Failure(rowNumber, "Hotel Address is required.");

        if (!item.Rating.HasValue)
            return Failure(rowNumber, "Rating is missing or invalid.");

        if (!item.PerNightRate.HasValue)
            return Failure(rowNumber, "PerNightRate is missing or invalid.");

        var country = (item.CountryIdentifier ?? item.Country)?.Trim();

        if (string.IsNullOrWhiteSpace(country))
            return Failure(rowNumber, "Country is required.");

        return Result<HotelImportRowDto>.Success(new HotelImportRowDto
        {
            RowNumber = rowNumber,
            Name = item.Name.Trim(),
            Address = item.Address.Trim(),
            Rating = item.Rating.Value,
            PerNightRate = item.PerNightRate.Value,
            CountryIdentifier = country
        });
    }

    private static Result<HotelImportRowDto> Failure(
        int rowNumber,
        string message) =>
        Result<HotelImportRowDto>.Failure(
            new Error(ErrorCodes.Validation, $"Row {rowNumber}: {message}"));

    private sealed record class HotelImportJsonModel(
        string? Name,
        string? Address,
        double? Rating,
        decimal? PerNightRate,
        string? Country,
        string? CountryIdentifier);
}