using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HotelListing.Api.Application.Contracts;
using HotelListing.Api.Application.MappingProfiles;
using HotelListing.Api.Application.Services;
using HotelListing.Api.CachePolicies;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Config;
using HotelListing.Api.Domain;
using HotelListing.Api.Handlers;
using Serilog;
using Serilog.Events;
using HotelListing.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Asp.Versioning;
using System.Reflection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.Extensions.DependencyInjection;

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

    // 1. DATABASE & PERSISTENCE
    builder.Services.AddDbContextPool<HotelListingDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("HotelListingDbConnectionString"), sqlOptions =>
        {
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null
            );
        });

        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    }, poolSize: 128);

    // 2. IDENTITY & USER STORE
    builder.Services.AddIdentityCore<ApplicationUser>()
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<HotelListingDbContext>();

    // 3. AUTHENTICATION & AUTHORIZATION
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
    var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>() ?? new JwtSettings();

    if (string.IsNullOrWhiteSpace(jwtSettings.Key))
    {
        Log.Fatal("JwtSettings:Key is not configured.");
        throw new InvalidOperationException("JwtSettings:key section is not configured.");
    }

    if (string.IsNullOrWhiteSpace(jwtSettings.Key))
        throw new InvalidOperationException("JwtSettings:Key is not configured.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
        ClockSkew = TimeSpan.Zero
    })
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>(AuthenticationDefaults.BasicScheme, _ => { })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(AuthenticationDefaults.ApiKeyScheme, _ => { });

    builder.Services.AddAuthorization();

    // 4. APPLICATION & INFRASTRUCTURE SERVICES
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(HotelMappingProfile).Assembly));

    builder.Services.AddScoped<ICountriesService, CountriesService>();
    builder.Services.AddScoped<IHotelsService, HotelsService>();
    builder.Services.AddScoped<IUsersService, UsersService>();
    builder.Services.AddScoped<IBookingService, BookingService>();
    builder.Services.AddScoped<IApiKeyValidatorService, ApiKeyValidatorService>();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    // 5. CACHING STRATEGY
    builder.Services.AddOutputCache(options =>
    {
        options.AddPolicy(CacheConstants.AuthenticatedUserCachingPolicy, policyBuilder =>
        {
            policyBuilder.AddPolicy<AuthenticatedUserCachingPolicy>()
                         .SetCacheKeyPrefix(CacheConstants.AuthenticatedUserCachingPolicyTag);
        }, excludeDefaultPolicy: true);
    });

    // 6. RATE LIMITING STRATEGY
    builder.Services.AddRateLimiter(options =>
    {
        // A. Fixed Window Policy (for simple endpoints)
        options.AddFixedWindowLimiter(RateLimitingConstants.FixedPolicy, opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 50;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit = 5;
        });

        // B. Sliding Window Policy (per authenticated user)
        options.AddPolicy(RateLimitingConstants.PerUserPolicy, context =>
        {
            var username = context.User?.FindFirst(ClaimTypes.Email)?.Value
                        ?? context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? context.User?.Identity?.Name
                        ?? RateLimitingConstants.AnonymousUser;

            return RateLimitPartition.GetSlidingWindowLimiter(username, _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 50,
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 3
            });
        });

        // C. Global Limiter (per client IP address)
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? RateLimitingConstants.UnknownIp;

            return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 200,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            });
        });

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, cancellationToken) =>
        {
            if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                context.HttpContext.Response.Headers.RetryAfter = retryAfter.TotalSeconds.ToString();

            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.HttpContext.Response.ContentType = "application/json";

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests",
                message = "Rate limit exceeded. Please try again later.",
                retryAfter = retryAfter.TotalSeconds
            }, cancellationToken: cancellationToken);
        };
    });

    // 7. CONTROLLERS & API SPECIFICATION
    builder.Services.AddControllers()
        .AddNewtonsoftJson()
        .AddJsonOptions(opt => opt.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

    builder.Services.AddOpenApi();

    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"), tags: ["api"])
        .AddDbContextCheck<HotelListingDbContext>("database", tags: ["db", "sql"]);

    builder.Services.AddApiVersioning(options =>
    {
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // API Information
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Hotel Listing API",
            Version = "v1",
            Description = "API for managing hotels, countries, and bookings",
            Contact = new OpenApiContact
            {
                Name = "Support Team",
                Email = "support@hotellisting.com",
            },
            License = new OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT"),
            }
        });

        options.SwaggerDoc("v2", new OpenApiInfo
        {
            Title = "Hotel Listing API V2",
            Version = "v2",
            Description = "Version 2 of the API for managing hotels, countries, and bookings",
        });

        // Include XML comments
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
            options.IncludeXmlComments(xmlPath);

        // Enable annotations
        options.EnableAnnotations();

        // Security Definitions
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter your token below.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT"
        });

        // API Key Authentication
        options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
        {
            Description = "API Key needed to access the API. X-API-Key: {API Key}",
            Name = "X-API-Key",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
        });

        // Basic Authentication
        options.AddSecurityDefinition("Basic", new OpenApiSecurityScheme
        {
            Description = "Basic Authentication using the Basic scheme",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Basic"
        });

        // Add operation filters for examples
        options.ExampleFilters();

        // Custom operation filter for handling multiple authentication schemes
        options.OperationFilter<SecurityRequirementsOperationFilter>(true, "Bearer");

        // Order action by method
        options.OrderActionsBy(api => $"{api.RelativePath}_{api.HttpMethod}");
    });

    builder.Services.AddSwaggerExamplesFromAssemblyOf<Program>();

    // APPLICATION PIPELINE (MIDDLEWARE)
    var app = builder.Build();

    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Listing API v1");
            options.SwaggerEndpoint("/swagger/v2/swagger.json", "Hotel Listing API v2");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Hotel Listing API Documentation";
            options.DisplayRequestDuration();
            options.EnableDeepLinking();
            options.EnableFilter();
            options.ShowExtensions();
            options.EnableValidator();
            options.EnablePersistAuthorization();
        });
    }

    app.UseHttpsRedirection();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });

    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("db")
    });

    // Authentication MUST precede Authorization and RateLimiter (to populate context.User)
    app.UseAuthentication();
    app.UseAuthorization();

    // Serilog’s HTTP Request Logging Middleware.
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";

        options.GetLevel = (httpContext, elapsed, ex) => ex != null
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 500
                ? LogEventLevel.Error
                : httpContext.Response.StatusCode >= 400
                    ? LogEventLevel.Warning
                    : LogEventLevel.Information;

        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            var username = httpContext.User?.Identity?.Name
                        ?? httpContext.User?.FindFirst(ClaimTypes.Email)?.Value
                        ?? "anonymous";

            diagnosticContext.Set("Username", username);
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? httpContext.User.FindFirst("sub")?.Value
                    ?? "unknown";
                diagnosticContext.Set("UserId", userId);
            }
        };
    });

    // Rate limiting and Output caching
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
