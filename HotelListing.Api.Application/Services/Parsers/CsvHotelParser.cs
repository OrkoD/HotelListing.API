using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Common.Constants;
using System.Globalization;
using System.Runtime.CompilerServices;
using CsvHelper.Configuration;
using CsvHelper;

namespace HotelListing.Api.Application.Services.Parsers;

/// <summary>
/// Streams and parses CSV hotel feeds line-by-line without loading entire files into memory.
/// </summary>
public sealed class CsvHotelParser : IHotelDataParser
{
    public SupportedImportFormat Format => SupportedImportFormat.Csv;

    public string SupportedExtension => ".csv";

    private const int ExpectedColumnCount = 5;

    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true,
        TrimOptions = TrimOptions.Trim,
        MissingFieldFound = null
    };

    public async IAsyncEnumerable<Result<HotelImportRowDto>> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, CsvConfig);

        // Read and validate header line
        if (!await csv.ReadAsync() || !csv.ReadHeader())
        {
            yield break;
        }

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowNumber = csv.Parser.Row; // CsvHelper tracks the exact physical line number!

            if (csv.Parser.Count != ExpectedColumnCount)
            {
                yield return Failure(rowNumber, $"Expected {ExpectedColumnCount} columns, but found {csv.Parser.Count}.");
                continue;
            }

            var name = csv.GetField<string>(0) ?? string.Empty;
            var address = csv.GetField<string>(1) ?? string.Empty;
            var country = csv.GetField<string>(4) ?? string.Empty;

            if (!csv.TryGetField<double>(2, out var rating))
            {
                yield return Failure(rowNumber, $"'{csv.GetField(2)}' is not a valid Rating.");
                continue;
            }

            if (!csv.TryGetField<decimal>(3, out var price))
            {
                yield return Failure(rowNumber, $"'{csv.GetField(3)}' is not a valid PerNightRate.");
                continue;
            }

            yield return Result<HotelImportRowDto>.Success(new HotelImportRowDto
            {
                RowNumber = rowNumber,
                Name = name,
                Address = address,
                Rating = rating,
                PerNightRate = price,
                CountryIdentifier = country
            });
        }
    }

    private static Result<HotelImportRowDto> Failure(int rowNumber, string message) =>
        Result<HotelImportRowDto>.Failure(
            new Error(ErrorCodes.Validation, $"Row {rowNumber}: {message}")
        );
}
