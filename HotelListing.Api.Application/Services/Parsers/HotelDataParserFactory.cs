using HotelListing.Api.Application.Contracts;

namespace HotelListing.Api.Application.Services.Parsers;

public class HotelDataParserFactory : IHotelDataParserFactory
{
    private readonly IReadOnlyDictionary<string, IHotelDataParser> _parsers;

    public HotelDataParserFactory(IEnumerable<IHotelDataParser> parsers)
    {
        _parsers = parsers.ToDictionary(
            p => NormalizeExtension(p.SupportedExtension),
            StringComparer.OrdinalIgnoreCase
        );
    }

    public IHotelDataParser? GetParser(string fileNameOrExtension)
    {
        var extension = Path.GetExtension(fileNameOrExtension);
        var key = string.IsNullOrEmpty(extension)
            ? fileNameOrExtension
            : extension;

        _parsers.TryGetValue(NormalizeExtension(key), out var parser);
        return parser;
    }

    private static string NormalizeExtension(string extension) =>
        extension.Trim().TrimStart('.');
}
