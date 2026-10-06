using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Filters;

namespace Microsoft.Extensions.DependencyInjection;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddOpenApi();

        services.AddSwaggerGen(options =>
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
                Scheme = JwtBearerDefaults.AuthenticationScheme,
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

        services.AddSwaggerExamplesFromAssemblies(Assembly.GetExecutingAssembly());

        return services;
    }

    public static IApplicationBuilder UseSwaggerDocumentation(this WebApplication app)
    {
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
        return app;
    }
}
