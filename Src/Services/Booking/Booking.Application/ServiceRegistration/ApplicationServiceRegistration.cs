using Booking.Application.Common.Behavior;

namespace Booking.Application.ServiceRegistration;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Mediator
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped; // optional
        });

        // Register Mappers
        services.AddScoped<IEventMapper, EventMapper>();
        services.AddScoped<IReservationMapper, ReservationMapper>();
        services.AddScoped<ISeatMapper, SeatMapper>();
        services.AddScoped<ISeatCategoryMapper, SeatCategoryMapper>();

        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
