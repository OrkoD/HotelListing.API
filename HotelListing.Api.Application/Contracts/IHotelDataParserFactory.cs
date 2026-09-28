namespace HotelListing.Api.Application.Contracts;

public interface IHotelDataParserFactory
{
    IHotelDataParser? GetParser(string fileNameOrExtension);
}
