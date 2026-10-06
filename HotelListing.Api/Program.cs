using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Conventions;
using HotelListing.Api.Middleware;
using HotelListing.Api.Services;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting HotelListing API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
    );

    // 1. Persistence & Identity
    builder.Services.AddPersistenceServices(builder.Configuration, builder.Environment);

    // 2. Authentication & Authorization
    builder.Services.AddAuthenticationServices(builder.Configuration);

    // 3. Application & Infrastructure Services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddScoped<IFileStorageService, AzureBlobStorageService>();
    builder.Services.AddApplicationServices();

    // 4. Cross-Cutting Services (Caching, Rate Limiting, Health Checks)
    builder.Services.AddCachingServices();
    builder.Services.AddRateLimitingServices();
    builder.Services.AddHealthCheckServices();

    // 5. API Presentation & Documentation
    builder.Services.AddControllers(options => options.Conventions.Add(new ApiControllerBaseConvention()))
        .AddNewtonsoftJson(opt =>
        {
            opt.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        })
        .AddJsonOptions(opt =>
        {
            opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            opt.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    builder.Services.AddApiVersioningServices();
    builder.Services.AddSwaggerDocumentation();

    // ==========================================
    // HTTP Request Pipeline (Middleware)
    // ==========================================
    var app = builder.Build();

    app.UseExceptionHandler();
    app.UseSwaggerDocumentation();
    app.UseHttpsRedirection();

    app.MapHealthCheckEndpoints();

    // Authentication MUST precede Authorization and RateLimiter
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseCustomSerilogRequestLogging();
    app.UseRateLimiter();
    app.UseOutputCache();

    app.MapControllers();

    Log.Information("HotelListing API started successfully");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

