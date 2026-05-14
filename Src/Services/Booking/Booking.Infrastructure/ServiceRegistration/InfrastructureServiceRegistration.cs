namespace Booking.Infrastructure.ServiceRegistration;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<BookingDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("BookingConnection") 
            )
        );

        // Repositories
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();

        // Payment Service
        services.AddScoped<IPaymentService, StripePaymentService>();

        // Background Services
        services.AddHostedService<HoldCleanupService>();

        return services;
    }
}



