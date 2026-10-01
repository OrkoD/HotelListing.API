using System.Globalization;
using System.Runtime.CompilerServices;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.DTOs.Hotel;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Results;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace HotelListing.Api.Application.Services.Parsers;

public sealed class PdfHotelParser : IHotelDataParser
{
    public SupportedImportFormat Format => SupportedImportFormat.Pdf;

    public string SupportedExtension => ".pdf";

    private const int ExpectedColumnCount = 5;

    public async IAsyncEnumerable<Result<HotelImportRowDto>> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 1. Open the PDF from the stream
        using var pdf = PdfDocument.Open(stream);

        var rowNumber = 0;

        // 2. Page-by-page streaming
        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 3. Extract text lines from the current page
            var lines = ExtractLinesFromPage(page);

            foreach (var line in lines)
            {
                // Skip headers, footers, or empty divider lines
                if (IsHeaderOrDivider(line))
                    continue;

                rowNumber++;

                // 4. Split line into columns delimited by '|'
                var columns = line.Split('|').Select(c => c.Trim()).ToArray();

                if (columns.Length != ExpectedColumnCount)
                {
                    yield return Result<HotelImportRowDto>.Failure(
                        new Error(ErrorCodes.Validation, $"Row {rowNumber}: Expected {ExpectedColumnCount} columns, found {columns.Length}."));
                    continue;
                }

                // 5. Parse Rating & Price with culture safety
                if (!double.TryParse(columns[2], CultureInfo.InvariantCulture, out var rating))
                {
                    yield return Result<HotelImportRowDto>.Failure(
                        new Error(ErrorCodes.Validation, $"Row {rowNumber}: '{columns[2]}' is not a valid Rating."));
                    continue;
                }

                if (!decimal.TryParse(columns[3], CultureInfo.InvariantCulture, out var price))
                {
                    yield return Result<HotelImportRowDto>.Failure(
                        new Error(ErrorCodes.Validation, $"Row {rowNumber}: '{columns[3]}' is not a valid PerNightRate."));
                    continue;
                }

                // 6. Yield typed DTO wrapped in Result
                yield return Result<HotelImportRowDto>.Success(new HotelImportRowDto
                {
                    RowNumber = rowNumber,
                    Name = columns[0],
                    Address = columns[1],
                    Rating = rating,
                    PerNightRate = price,
                    CountryIdentifier = columns[4]
                });
            }
        }
    }

    private static IEnumerable<string> ExtractLinesFromPage(Page page)
    {
        if (page.Text.Contains('\n'))
        {
            return page.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        }

        var words = page.GetWords()
            .OrderByDescending(w => w.BoundingBox.Bottom)
            .ThenBy(w => w.BoundingBox.Left)
            .ToList();

        if (words.Count == 0)
            return [];

        var lines = new List<string>();
        var currentLineWords = new List<Word> { words[0] };
        var currentY = words[0].BoundingBox.Bottom;

        for (var i = 1; i < words.Count; i++)
        {
            var word = words[i];
            // Words within vertical tolerance (4 points) belong to the same line
            if (Math.Abs(word.BoundingBox.Bottom - currentY) <= 4)
            {
                currentLineWords.Add(word);
            }
            else
            {
                lines.Add(string.Join(" ", currentLineWords.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
                currentLineWords = [word];
                currentY = word.BoundingBox.Bottom;
            }
        }

        if (currentLineWords.Count > 0)
        {
            lines.Add(string.Join(" ", currentLineWords.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)));
        }

        return lines;
    }

    private static bool IsHeaderOrDivider(string line)
    {
        var trimmed = line.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
            return true;

        // Skip divider rules like "-----" or "====="
        if (trimmed.All(c => c is '-' or '=' or '_' or '+' or ' '))
            return true;

        // Skip report titles or footers without the column delimiter '|'
        if (!trimmed.Contains('|'))
            return true;

        // Skip table column headers
        if (trimmed.Contains("Rating", StringComparison.OrdinalIgnoreCase) &&
            trimmed.Contains("Price", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
