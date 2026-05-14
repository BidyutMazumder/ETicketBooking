using Booking.API.Hubs;
using Booking.API.Middleware;
using Booking.API.ServiceRegistration;
using Booking.Application.ServiceRegistration;
using Booking.Infrastructure.ServiceRegistration;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (!builder.Environment.IsProduction())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}

// Add services to the container
builder.Services.AddApiServices();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Booking API v1.0");
        c.DefaultModelsExpandDepth(2);
        c.DefaultModelExpandDepth(2);
    });
}

app.UseHttpsRedirection();

// Middleware pipeline (order matters)
app.UseMiddleware<AuditLoggingMiddleware>();      // Log all requests
app.UseMiddleware<RateLimitingMiddleware>();      // Rate limiting
app.UseMiddleware<JwtValidationMiddleware>();     // JWT validation
app.UseMiddleware<GlobalExceptionMiddleware>();   // Exception handling

app.UseRouting();
app.UseCors("AllowAll");

// Configure SignalR endpoints
app.UseWebSockets();
app.MapHub<BookingHub>("/hubs/booking", options =>
{
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                        Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
});

app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Booking API starting up...");
logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
logger.LogInformation("Swagger UI available at: /swagger/index.html");
logger.LogInformation("SignalR BookingHub available at: /hubs/booking");

app.Run();
