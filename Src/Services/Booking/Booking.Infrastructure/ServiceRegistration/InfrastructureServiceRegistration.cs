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
        services.AddScoped<ISeatCategoryRepository, SeatCategoryRepository>();

        // Payment Service
        services.AddScoped<IPaymentService, StripePaymentService>();

        // Background Services
        services.AddHostedService<HoldCleanupService>();

        return services;
    }
}



