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

    // APPLICATION PIPELINE (MIDDLEWARE)
    var app = builder.Build();

    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/openapi/v1.json", "HotelListing.Api v1");
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
