namespace Identity.Application.ServiceRegistration;
using Identity.Application.Common.Behaviors;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers application layer services with the dependency injection container.
/// This includes MediatR handlers, validators, and mappings.
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR with handlers from this assembly
        services.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Scoped; // optional
        });
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceRegistration).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Register Mappings
        services.AddScoped<IUserMapper, UserMapper>();

        return services;
    }
}
