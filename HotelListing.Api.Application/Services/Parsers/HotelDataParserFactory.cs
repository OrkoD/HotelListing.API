using HotelListing.Api.Application.Contracts;

namespace HotelListing.Api.Application.Services.Parsers;

public class HotelDataParserFactory(IEnumerable<IHotelDataParser> parsers) : IHotelDataParserFactory
{
    public IHotelDataParser? GetParser(string fileNameOrExtension)
    {
        var extension = Path.GetExtension(fileNameOrExtension).ToLowerInvariant();

        var format = extension switch
        {
            ".csv" => SupportedImportFormat.Csv,
            ".json" => SupportedImportFormat.Json,
            ".pdf" => SupportedImportFormat.Pdf,
            _ => (SupportedImportFormat?)null
        };

        if (format is null)
            return null;

        // Finds the registered parser matching this format from the DI collection
        return parsers.FirstOrDefault(x => x.Format == format);
    }
}
