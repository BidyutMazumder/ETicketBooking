using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Booking.API.ServiceRegistration;

public static class ApiServiceRegistration
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "E-Ticket Booking Service API",
                Version = "v1.0",
                Description = "RESTful API for event management and seat reservations with high-concurrency support",
                Contact = new OpenApiContact
                {
                    Name = "Development Team",
                    Email = "dev@eticketbooking.com",
                },
                License = new OpenApiLicense
                {
                    Name = "MIT License",
                },
            });

            // Add JWT authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            });

            options.AddSecurityRequirement(document =>
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer", document),
                        new List<string>()
                    }
                });

            // Add XML documentation comments
            var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Enable lowercase routes
            options.SchemaGeneratorOptions = new SchemaGeneratorOptions { UseInlineDefinitionsForEnums = true };
        });

        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // SignalR for real-time updates
        services.AddSignalR(options =>
        {
            options.MaximumReceiveMessageSize = 32 * 1024; // 32KB max message size
            options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
        });

        // Real-time Notifications
        services.AddScoped<Booking.Application.Common.Interfaces.IRealtimeNotificationService, 
                           Booking.API.Services.RealtimeNotificationService>();

        return services;
    }
}
