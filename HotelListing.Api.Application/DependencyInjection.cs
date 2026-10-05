using System.Reflection;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.Services;
using HotelListing.Api.Application.Services.Parsers;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 1. AutoMapper (registers all profiles in this assembly)
        services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

        // 2. Core Business Services
        services.AddScoped<ICountriesService, CountriesService>();
        services.AddScoped<IHotelsService, HotelsService>();
        services.AddScoped<IUsersService, UsersService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IApiKeyValidatorService, ApiKeyValidatorService>();
        services.AddScoped<IHotelImportService, HotelImportService>();

        // 3. Document Ingestion Parsers (Strategy + Factory)
        services.AddScoped<IHotelDataParser, CsvHotelParser>();
        services.AddScoped<IHotelDataParser, PdfHotelParser>();
        services.AddScoped<IHotelDataParser, JsonHotelParser>();
        services.AddScoped<IHotelDataParserFactory, HotelDataParserFactory>();

        return services;
    }
}
